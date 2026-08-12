using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorConnect.Application.Features.Bookings.DTOs;
using TutorConnect.Application.Services;
using TutorConnect.Infrastructure.SqlServer.Persistence;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Enums;

namespace TutorConnect.Infrastructure.SqlServer.Services
{
    internal class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _dbContext;

        public BookingService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Helper kiểm tra trùng lịch dạy/học (StartA < EndB && EndA > StartB)
        private async Task<bool> HasScheduleConflictAsync(
            long tutorSubjectId,
            long studentId,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            long? excludeBookingId = null,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Booking>()
                .AnyAsync(b => (excludeBookingId == null || b.Id != excludeBookingId)
                    && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                    && (b.TutorSubjectId == tutorSubjectId || b.StudentId == studentId)
                    && (startTimeUtc < b.EndTimeUtc && endTimeUtc > b.StartTimeUtc), cancellationToken);
        }

        public async Task<BookingMinimal> CreateBookingAsync(BookingCreateRequest request, long studentId, CancellationToken cancellationToken = default)
        {
            var tutorSubject = await _dbContext.Set<TutorSubject>()
                .Include(ts => ts.Tutor)
                .FirstOrDefaultAsync(ts => ts.Id == request.TutorSubjectId, cancellationToken);

            if (tutorSubject == null)
                throw new KeyNotFoundException("TutorSubject not found");

            var hasConflict = await HasScheduleConflictAsync(
                request.TutorSubjectId, studentId, request.StartTimeUtc, request.EndTimeUtc, null, cancellationToken);

            if (hasConflict)
                throw new InvalidOperationException("Booking time conflict");

            var creditCost = (int?)null;
            try { creditCost = tutorSubject.FeePerSessionCredits; } catch { creditCost = 1; }

            var booking = new Booking(studentId, request.TutorSubjectId, request.StartTimeUtc, request.EndTimeUtc, creditCost ?? 1, request.StudentNote);

            await _dbContext.Set<Booking>().AddAsync(booking, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new BookingMinimal(booking.Id, booking.StudentId, booking.TutorSubjectId, booking.StartTimeUtc, booking.EndTimeUtc, booking.CreditCost, booking.Status, booking.MeetingUrl);
        }

        public async Task<RescheduleProposalDto> CreateRescheduleProposalAsync(long bookingId, long userId, RescheduleCreateRequest request, CancellationToken cancellationToken = default)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var booking = await _dbContext.Set<Booking>()
                    .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

                if (booking == null)
                    throw new KeyNotFoundException("Booking not found");

                // Kiểm tra trùng lịch thời gian mới
                var hasConflict = await HasScheduleConflictAsync(
                    booking.TutorSubjectId, booking.StudentId, request.NewStartTimeUtc, request.NewEndTimeUtc, booking.Id, cancellationToken);

                if (hasConflict)
                    throw new InvalidOperationException("Reschedule time conflict detected");

                // Giả định RescheduleProposal entity hoặc tạo DTO phản hồi
                var proposalId = DateTime.UtcNow.Ticks; // Hoặc ID tự tăng từ DB

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new RescheduleProposalDto(
                    proposalId,
                    bookingId,
                    userId,
                    request.NewStartTimeUtc,
                    request.NewEndTimeUtc,
                    "Pending",
                    request.Reason
                );
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<BookingMinimal> RespondToRescheduleProposalAsync(long bookingId, long proposalId, long userId, RescheduleStatusUpdateRequest request, CancellationToken cancellationToken = default)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var booking = await _dbContext.Set<Booking>()
                    .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

                if (booking == null)
                    throw new KeyNotFoundException("Booking not found");

                if (request.IsApproved)
                {
                    // Cập nhật lại thời gian Booking nếu chấp nhận
                    // booking.UpdateSchedule(newStartTime, newEndTime);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return new BookingMinimal(booking.Id, booking.StudentId, booking.TutorSubjectId, booking.StartTimeUtc, booking.EndTimeUtc, booking.CreditCost, booking.Status, booking.MeetingUrl);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}