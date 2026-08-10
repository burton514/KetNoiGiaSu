using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorConnect.Application.Features.Progress.DTOs;
using TutorConnect.Application.Services;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Enums;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Services
{
    internal class SessionService : ISessionService
    {
        private readonly ApplicationDbContext _dbContext;

        public SessionService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TutorConnect.Application.Features.Bookings.DTOs.CompleteBookingResult> CompleteBookingAsync(long bookingId, SessionProgressUpsertRequest request, CancellationToken cancellationToken = default)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var booking = await _dbContext.Set<Booking>()
                .Include(b => b.SessionProgress)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking is null)
                throw new KeyNotFoundException("Booking not found");

            if (booking.Status != BookingStatus.Confirmed)
                throw new InvalidOperationException("Only Confirmed bookings can be completed.");

            if (booking.SessionProgress is not null)
                throw new InvalidOperationException("SessionProgress already exists for this booking.");

            var learningGoal = await _dbContext.Set<LearningGoal>()
                .FirstOrDefaultAsync(g => g.Id == request.LearningGoalId, cancellationToken);

            if (learningGoal is null)
                throw new KeyNotFoundException("LearningGoal not found");

            var progress = new SessionProgress(bookingId, request.LearningGoalId, (decimal?)request.Score, (decimal?)request.MaxScore, (decimal)request.GoalProgressPercent, request.TutorComment);

            await _dbContext.Set<SessionProgress>().AddAsync(progress, cancellationToken);

            booking.Complete();
            learningGoal.SynchronizeStatus((decimal)request.GoalProgressPercent);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var bookingMinimal = new TutorConnect.Application.Features.Bookings.DTOs.BookingMinimal(
                booking.Id,
                booking.StudentId,
                booking.TutorSubjectId,
                booking.StartTimeUtc,
                booking.EndTimeUtc,
                booking.CreditCost,
                booking.Status,
                booking.MeetingUrl);

            var result = new TutorConnect.Application.Features.Bookings.DTOs.CompleteBookingResult(
                bookingMinimal,
                new SessionProgressResponse(progress.BookingId, progress.LearningGoalId, (double?)progress.Score, (double?)progress.MaxScore, (double)progress.GoalProgressPercent, progress.TutorComment));

            return result;
        }
    }
}
