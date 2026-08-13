using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TutorConnect.Application.Common.Exceptions;
using TutorConnect.Application.Features.Bookings.DTOs;
using TutorConnect.Application.Services;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Enums;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Services
{
    internal class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TimeSpan _minimumNotice;

        public BookingService(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;

            var configuredNotice = configuration["BookingRules:MinimumNoticeHours"];
            _minimumNotice = int.TryParse(configuredNotice, out var noticeHours) && noticeHours > 0
                ? TimeSpan.FromHours(noticeHours)
                : TimeSpan.FromHours(12);
        }

        private async Task<bool> HasScheduleConflictAsync(
            long tutorId,
            long studentId,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            long? excludeBookingId = null,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Bookings
                .AnyAsync(
                    b => (excludeBookingId == null || b.Id != excludeBookingId)
                        && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                        && (b.StudentId == studentId || b.TutorSubject.TutorId == tutorId)
                        && startTimeUtc < b.EndTimeUtc
                        && endTimeUtc > b.StartTimeUtc,
                    cancellationToken);
        }

        public async Task<BookingMinimal> CreateBookingAsync(
            BookingCreateRequest request,
            long studentId,
            CancellationToken cancellationToken = default)
        {
            var startTimeUtc = NormalizeUtc(request.StartTimeUtc);
            var endTimeUtc = NormalizeUtc(request.EndTimeUtc);
            EnsureFuturePeriod(startTimeUtc, endTimeUtc);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var student = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken);

            if (student is null)
            {
                throw new NotFoundException("Student not found.");
            }

            if (student.Role != UserRole.Student || student.Status != UserStatus.Active)
            {
                throw new ForbiddenException("Only an active Student can create a booking.");
            }

            var tutorSubject = await _dbContext.TutorSubjects
                .Include(ts => ts.Subject)
                .Include(ts => ts.Tutor)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(ts => ts.Id == request.TutorSubjectId, cancellationToken);

            if (tutorSubject is null)
            {
                throw new NotFoundException("TutorSubject not found.");
            }

            EnsureTutorEligible(tutorSubject);

            await EnsureWithinTutorAvailabilityAsync(
                tutorSubject.TutorId,
                tutorSubject.Tutor.User.TimeZoneId,
                startTimeUtc,
                endTimeUtc,
                cancellationToken);

            var hasConflict = await HasScheduleConflictAsync(
                tutorSubject.TutorId,
                studentId,
                startTimeUtc,
                endTimeUtc,
                cancellationToken: cancellationToken);

            if (hasConflict)
            {
                throw new InvalidOperationException("Booking time conflict.");
            }

            var booking = new Booking(
                studentId,
                request.TutorSubjectId,
                startTimeUtc,
                endTimeUtc,
                tutorSubject.FeePerSessionCredits,
                request.StudentNote);

            await _dbContext.Bookings.AddAsync(booking, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ToMinimal(booking);
        }

        public async Task<RescheduleProposalDto> CreateRescheduleProposalAsync(
            long bookingId,
            long userId,
            RescheduleCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var newStartTimeUtc = NormalizeUtc(request.NewStartTimeUtc);
            var newEndTimeUtc = NormalizeUtc(request.NewEndTimeUtc);
            EnsureFuturePeriod(newStartTimeUtc, newEndTimeUtc);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var booking = await _dbContext.Bookings
                .Include(b => b.TutorSubject)
                    .ThenInclude(ts => ts.Tutor)
                        .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException("Booking not found.");
            }

            EnsureBookingCanBeRescheduled(booking);
            EnsureBookingParty(booking, userId);
            EnsureTutorCanOperate(booking.TutorSubject.Tutor);
            EnsureMinimumNotice(booking.StartTimeUtc);

            var hasPendingRequest = await _dbContext.RescheduleRequests
                .AnyAsync(
                    r => r.BookingId == bookingId && r.Status == RescheduleRequestStatus.Pending,
                    cancellationToken);

            if (hasPendingRequest)
            {
                throw new InvalidOperationException("This booking already has a pending reschedule request.");
            }

            await EnsureWithinTutorAvailabilityAsync(
                booking.TutorSubject.TutorId,
                booking.TutorSubject.Tutor.User.TimeZoneId,
                newStartTimeUtc,
                newEndTimeUtc,
                cancellationToken);

            var hasConflict = await HasScheduleConflictAsync(
                booking.TutorSubject.TutorId,
                booking.StudentId,
                newStartTimeUtc,
                newEndTimeUtc,
                booking.Id,
                cancellationToken);

            if (hasConflict)
            {
                throw new InvalidOperationException("Reschedule time conflict detected.");
            }

            var proposal = new RescheduleRequest(
                booking.Id,
                userId,
                booking.StartTimeUtc,
                booking.EndTimeUtc,
                newStartTimeUtc,
                newEndTimeUtc,
                DateTime.UtcNow,
                request.Reason);

            await _dbContext.RescheduleRequests.AddAsync(proposal, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RescheduleProposalDto(
                proposal.Id,
                proposal.BookingId,
                proposal.RequestedByUserId,
                proposal.ProposedStartTimeUtc,
                proposal.ProposedEndTimeUtc,
                proposal.Status.ToString(),
                proposal.Reason);
        }

        public async Task<BookingMinimal> RespondToRescheduleProposalAsync(
            long bookingId,
            long proposalId,
            long userId,
            RescheduleStatusUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var proposal = await _dbContext.RescheduleRequests
                .Include(r => r.Booking)
                    .ThenInclude(b => b.TutorSubject)
                        .ThenInclude(ts => ts.Tutor)
                            .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(
                    r => r.Id == proposalId && r.BookingId == bookingId,
                    cancellationToken);

            if (proposal is null)
            {
                throw new NotFoundException("Reschedule request not found.");
            }

            var booking = proposal.Booking;
            EnsureBookingCanBeRescheduled(booking);
            EnsureBookingParty(booking, userId);

            if (proposal.RequestedByUserId == userId)
            {
                throw new ForbiddenException("The requester cannot respond to their own reschedule request.");
            }

            if (proposal.Status != RescheduleRequestStatus.Pending)
            {
                throw new InvalidOperationException("Only a Pending reschedule request can be processed.");
            }

            if (userId == booking.TutorSubject.TutorId)
            {
                EnsureTutorCanOperate(booking.TutorSubject.Tutor);
            }

            if (request.IsApproved)
            {
                EnsureTutorCanOperate(booking.TutorSubject.Tutor);
                EnsureMinimumNotice(booking.StartTimeUtc);
                EnsureFuturePeriod(proposal.ProposedStartTimeUtc, proposal.ProposedEndTimeUtc);

                await EnsureWithinTutorAvailabilityAsync(
                    booking.TutorSubject.TutorId,
                    booking.TutorSubject.Tutor.User.TimeZoneId,
                    proposal.ProposedStartTimeUtc,
                    proposal.ProposedEndTimeUtc,
                    cancellationToken);

                var hasConflict = await HasScheduleConflictAsync(
                    booking.TutorSubject.TutorId,
                    booking.StudentId,
                    proposal.ProposedStartTimeUtc,
                    proposal.ProposedEndTimeUtc,
                    booking.Id,
                    cancellationToken);

                if (hasConflict)
                {
                    throw new InvalidOperationException("Reschedule time conflict detected.");
                }

                booking.ChangeSchedule(proposal.ProposedStartTimeUtc, proposal.ProposedEndTimeUtc);
                proposal.Accept(userId, request.Note);
            }
            else
            {
                proposal.Reject(userId, request.Note);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ToMinimal(booking);
        }

        private async Task EnsureWithinTutorAvailabilityAsync(
            long tutorId,
            string timeZoneId,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            CancellationToken cancellationToken)
        {
            startTimeUtc = NormalizeUtc(startTimeUtc);
            endTimeUtc = NormalizeUtc(endTimeUtc);

            TimeZoneInfo timeZone;
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new InvalidOperationException($"Tutor time zone '{timeZoneId}' is not supported by the server.");
            }
            catch (InvalidTimeZoneException)
            {
                throw new InvalidOperationException($"Tutor time zone '{timeZoneId}' is invalid.");
            }

            var localStart = TimeZoneInfo.ConvertTimeFromUtc(startTimeUtc, timeZone);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endTimeUtc, timeZone);

            if (localStart.Date != localEnd.Date || localStart.DayOfWeek != localEnd.DayOfWeek)
            {
                throw new InvalidOperationException(
                    "Booking must fit completely inside one active weekly tutor availability window.");
            }

            var localStartTime = TimeOnly.FromDateTime(localStart);
            var localEndTime = TimeOnly.FromDateTime(localEnd);
            var dayOfWeek = localStart.DayOfWeek;

            var isAvailable = await _dbContext.TutorAvailabilities
                .AnyAsync(
                    a => a.TutorId == tutorId
                        && a.IsActive
                        && a.DayOfWeek == dayOfWeek
                        && a.StartTime <= localStartTime
                        && a.EndTime >= localEndTime,
                    cancellationToken);

            if (!isAvailable)
            {
                throw new InvalidOperationException(
                    "Booking must fit completely inside an active tutor availability window.");
            }
        }

        private static void EnsureTutorEligible(TutorSubject tutorSubject)
        {
            if (!tutorSubject.IsActive)
            {
                throw new InvalidOperationException("TutorSubject is inactive.");
            }

            if (!tutorSubject.Subject.IsActive)
            {
                throw new InvalidOperationException("Subject is inactive.");
            }

            if (tutorSubject.Tutor.User.Role != UserRole.Tutor
                || tutorSubject.Tutor.User.Status != UserStatus.Active
                || tutorSubject.Tutor.ApprovalStatus != TutorApprovalStatus.Approved)
            {
                throw new InvalidOperationException("Tutor is not approved and active.");
            }
        }

        private static void EnsureTutorCanOperate(TutorProfile tutorProfile)
        {
            if (tutorProfile.User.Role != UserRole.Tutor
                || tutorProfile.User.Status != UserStatus.Active
                || tutorProfile.ApprovalStatus != TutorApprovalStatus.Approved)
            {
                throw new ForbiddenException("Tutor must be active and approved to perform this action.");
            }
        }

        private void EnsureMinimumNotice(DateTime bookingStartTimeUtc)
        {
            if (NormalizeUtc(bookingStartTimeUtc) - DateTime.UtcNow < _minimumNotice)
            {
                throw new InvalidOperationException(
                    $"Cancellation or rescheduling requires at least {_minimumNotice.TotalHours:0} hours notice.");
            }
        }

        private static void EnsureBookingCanBeRescheduled(Booking booking)
        {
            if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
            {
                throw new InvalidOperationException("Only Pending or Confirmed bookings can be rescheduled.");
            }
        }

        private static void EnsureBookingParty(Booking booking, long userId)
        {
            if (booking.StudentId != userId && booking.TutorSubject.TutorId != userId)
            {
                throw new ForbiddenException("Only the Student or Tutor of this booking can perform this action.");
            }
        }

        private static void EnsureFuturePeriod(DateTime startTimeUtc, DateTime endTimeUtc)
        {
            if (endTimeUtc <= startTimeUtc)
            {
                throw new ArgumentException("End time must be later than start time.");
            }

            if (startTimeUtc <= DateTime.UtcNow)
            {
                throw new ArgumentException("Booking time must be in the future.");
            }
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

        private static BookingMinimal ToMinimal(Booking booking)
        {
            return new BookingMinimal(
                booking.Id,
                booking.StudentId,
                booking.TutorSubjectId,
                booking.StartTimeUtc,
                booking.EndTimeUtc,
                booking.CreditCost,
                booking.Status,
                booking.MeetingUrl);
        }
    }
}
