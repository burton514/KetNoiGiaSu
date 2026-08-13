using Microsoft.EntityFrameworkCore;
using TutorConnect.Application.Common.Exceptions;
using TutorConnect.Application.Features.Availability.DTOs;
using TutorConnect.Application.Features.Matching.DTOs;
using TutorConnect.Application.Services;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Enums;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Services
{
    internal sealed class MatchingService : IMatchingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TutorMetricsProvider _metricsProvider;

        public MatchingService(ApplicationDbContext dbContext, TutorMetricsProvider metricsProvider)
        {
            _dbContext = dbContext;
            _metricsProvider = metricsProvider;
        }

        public async Task<IReadOnlyList<TutorSearchItemResponse>> SearchTutorsAsync(
            TutorSearchRequest request,
            long studentId,
            CancellationToken cancellationToken = default)
        {
            if (request.SubjectId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.SubjectId), "SubjectId must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.TeachingLevel))
            {
                throw new ArgumentException("TeachingLevel is required.", nameof(request.TeachingLevel));
            }

            if (request.TeachingLevel.Trim().Length > 100)
            {
                throw new ArgumentException(
                    "TeachingLevel must not exceed 100 characters.",
                    nameof(request.TeachingLevel));
            }

            var startTimeUtc = NormalizeUtc(request.StartTimeUtc);
            var endTimeUtc = NormalizeUtc(request.EndTimeUtc);
            if (endTimeUtc <= startTimeUtc)
            {
                throw new ArgumentException("EndTimeUtc must be later than StartTimeUtc.");
            }

            if (startTimeUtc <= DateTime.UtcNow)
            {
                throw new ArgumentException("The requested matching window must be in the future.");
            }

            var student = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken);

            if (student is null)
            {
                throw new NotFoundException("Student not found.");
            }

            if (student.Role != UserRole.Student || student.Status != UserStatus.Active)
            {
                throw new ForbiddenException("Only an active Student can search for matching tutors.");
            }

            var subject = await _dbContext.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.SubjectId, cancellationToken);

            if (subject is null)
            {
                throw new NotFoundException("Subject not found.");
            }

            if (!subject.IsActive)
            {
                return Array.Empty<TutorSearchItemResponse>();
            }

            var studentHasConflict = await _dbContext.Bookings
                .AsNoTracking()
                .AnyAsync(
                    b => b.StudentId == studentId
                        && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                        && startTimeUtc < b.EndTimeUtc
                        && endTimeUtc > b.StartTimeUtc,
                    cancellationToken);

            if (studentHasConflict)
            {
                throw new InvalidOperationException("The Student already has a booking that conflicts with the requested time.");
            }

            var teachingLevel = request.TeachingLevel.Trim();
            var query = _dbContext.TutorSubjects
                .AsNoTracking()
                .Include(ts => ts.Subject)
                .Include(ts => ts.Tutor)
                    .ThenInclude(t => t.User)
                .Include(ts => ts.Tutor)
                    .ThenInclude(t => t.TutorAvailabilities)
                .Where(ts => ts.IsActive
                    && ts.SubjectId == request.SubjectId
                    && ts.Subject.IsActive
                    && ts.TeachingLevel == teachingLevel
                    && ts.Tutor.User.Role == UserRole.Tutor
                    && ts.Tutor.User.Status == UserStatus.Active
                    && ts.Tutor.ApprovalStatus == TutorApprovalStatus.Approved);

            var candidates = await query.ToListAsync(cancellationToken);
            candidates = candidates
                .Where(ts => IsCoveredByWeeklyAvailability(ts.Tutor, startTimeUtc, endTimeUtc))
                .ToList();

            if (candidates.Count == 0)
            {
                return Array.Empty<TutorSearchItemResponse>();
            }

            var tutorIds = candidates.Select(ts => ts.TutorId).Distinct().ToArray();
            var busyTutorIds = await _dbContext.Bookings
                .AsNoTracking()
                .Where(b => tutorIds.Contains(b.TutorSubject.TutorId)
                    && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                    && startTimeUtc < b.EndTimeUtc
                    && endTimeUtc > b.StartTimeUtc)
                .Select(b => b.TutorSubject.TutorId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var busySet = busyTutorIds.ToHashSet();
            candidates = candidates.Where(ts => !busySet.Contains(ts.TutorId)).ToList();

            if (candidates.Count == 0)
            {
                return Array.Empty<TutorSearchItemResponse>();
            }

            var metrics = await _metricsProvider.GetAsync(
                candidates.Select(ts => ts.TutorId),
                cancellationToken);

            return candidates
                .Select(ts =>
                {
                    var metric = metrics.TryGetValue(ts.TutorId, out var value)
                        ? value
                        : new TutorMetrics(0d, 0, 50d);

                    return new TutorSearchItemResponse(
                        ts.TutorId,
                        ts.Tutor.User.FullName,
                        ts.Tutor.Bio,
                        ts.Tutor.Qualification,
                        ts.Tutor.ExperienceYears,
                        ts.SubjectId,
                        ts.Subject.Name,
                        ts.TeachingLevel,
                        ts.FeePerSessionCredits,
                        metric.AverageRating,
                        metric.ReviewCount,
                        100d,
                        new AvailableWindowResponse(startTimeUtc, endTimeUtc),
                        metric.ReputationScore);
                })
                .OrderByDescending(x => x.ReputationScore)
                .ThenByDescending(x => x.AverageRating)
                .ThenByDescending(x => x.ReviewCount)
                .ThenBy(x => x.FeePerSessionCredits)
                .ThenByDescending(x => x.ExperienceYears)
                .ThenBy(x => x.FullName)
                .ToList();
        }

        private static bool IsCoveredByWeeklyAvailability(
            TutorProfile tutor,
            DateTime startTimeUtc,
            DateTime endTimeUtc)
        {
            TimeZoneInfo timeZone;
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(tutor.User.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
            catch (InvalidTimeZoneException)
            {
                return false;
            }

            var localStart = TimeZoneInfo.ConvertTimeFromUtc(startTimeUtc, timeZone);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endTimeUtc, timeZone);
            if (localStart.Date != localEnd.Date || localStart.DayOfWeek != localEnd.DayOfWeek)
            {
                return false;
            }

            var startTime = TimeOnly.FromDateTime(localStart);
            var endTime = TimeOnly.FromDateTime(localEnd);

            return tutor.TutorAvailabilities.Any(a => a.IsActive
                && a.DayOfWeek == localStart.DayOfWeek
                && a.StartTime <= startTime
                && a.EndTime >= endTime);
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
