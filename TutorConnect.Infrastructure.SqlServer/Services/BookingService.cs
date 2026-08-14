using Microsoft.EntityFrameworkCore;
using TutorConnect.Application.Services;
using TutorConnect.Application.Features.Bookings.DTOs;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Enums;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tạo mới Lịch học (Có kiểm tra trùng lịch trước khi tạo)
        /// </summary>
        public async Task<object> CreateBookingAsync(
            BookingCreateRequest request,
            long studentId,
            CancellationToken cancellationToken = default)
        {
            // 1. Kiểm tra xem học viên hoặc gia sư có bị trùng lịch khung giờ này không
            bool hasConflict = await HasScheduleConflictAsync(
                studentId,
                request.StartTimeUtc,
                request.EndTimeUtc,
                cancellationToken: cancellationToken);

            if (hasConflict)
            {
                throw new InvalidOperationException("The requested time slot conflicts with an existing booking.");
            }

            // 2. Khởi tạo Entity Booking
            var booking = new Booking(
                studentId: studentId,
                tutorSubjectId: request.TutorSubjectId,
                startTimeUtc: request.StartTimeUtc,
                endTimeUtc: request.EndTimeUtc,
                creditCost: request.CreditCost,
                studentNote: request.StudentNote
            );

            _context.Set<Booking>().Add(booking);
            await _context.SaveChangesAsync(cancellationToken);

            return new
            {
                booking.Id,
                booking.StudentId,
                booking.TutorSubjectId,
                booking.StartTimeUtc,
                booking.EndTimeUtc,
                booking.CreditCost,
                Status = booking.Status.ToString(),
                booking.StudentNote
            };
        }

        /// <summary>
        /// Kiểm tra xung đột thời gian (StartA < EndB AND EndA > StartB)
        /// </summary>
        public async Task<bool> HasScheduleConflictAsync(
            long userId,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            long? excludeBookingId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Booking>()
                .AsNoTracking()
                .Where(b => (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                            && (b.StudentId == userId || b.TutorSubject.TutorId == userId)
                            && b.StartTimeUtc < endTimeUtc
                            && b.EndTimeUtc > startTimeUtc);

            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.Id != excludeBookingId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        /// <summary>
        /// Tạo đề xuất đổi lịch mới (Reschedule Proposal)
        /// </summary>
        public async Task<RescheduleResponse> CreateRescheduleRequestAsync(
            long bookingId,
            long currentUserId,
            RescheduleCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var booking = await _context.Set<Booking>()
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                throw new KeyNotFoundException($"Booking with ID {bookingId} not found.");
            }

            bool hasConflict = await HasScheduleConflictAsync(
                currentUserId,
                request.ProposedStartTimeUtc,
                request.ProposedEndTimeUtc,
                excludeBookingId: bookingId,
                cancellationToken: cancellationToken);

            if (hasConflict)
            {
                throw new InvalidOperationException("The proposed time slot conflicts with an existing booking.");
            }

            var rescheduleEntity = new RescheduleRequest(
                bookingId: booking.Id,
                requestedByUserId: currentUserId,
                originalStartTimeUtc: booking.StartTimeUtc,
                originalEndTimeUtc: booking.EndTimeUtc,
                proposedStartTimeUtc: request.ProposedStartTimeUtc,
                proposedEndTimeUtc: request.ProposedEndTimeUtc,
                requestedAtUtc: DateTime.UtcNow,
                reason: request.Reason
            );

            _context.Set<RescheduleRequest>().Add(rescheduleEntity);
            await _context.SaveChangesAsync(cancellationToken);

            return MapToResponse(rescheduleEntity);
        }

        /// <summary>
        /// Phê duyệt (Approve) hoặc Từ chối (Reject) đề xuất đổi lịch
        /// </summary>
        public async Task<RescheduleResponse> RespondToRescheduleAsync(
            long bookingId,
            long proposalId,
            long currentUserId,
            RescheduleStatusUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var rescheduleEntity = await _context.Set<RescheduleRequest>()
                    .Include(r => r.Booking)
                    .FirstOrDefaultAsync(r => r.Id == proposalId && r.BookingId == bookingId, cancellationToken);

                if (rescheduleEntity == null)
                {
                    throw new KeyNotFoundException($"Reschedule request with ID {proposalId} for booking {bookingId} was not found.");
                }

                var booking = rescheduleEntity.Booking;

                if (request.Status == RescheduleStatusAction.Approve)
                {
                    rescheduleEntity.Accept(currentUserId, request.ResponseNote);

                    bool hasConflict = await HasScheduleConflictAsync(
                        currentUserId,
                        rescheduleEntity.ProposedStartTimeUtc,
                        rescheduleEntity.ProposedEndTimeUtc,
                        excludeBookingId: booking.Id,
                        cancellationToken: cancellationToken);

                    if (hasConflict)
                    {
                        throw new InvalidOperationException("Cannot approve reschedule request because of a schedule conflict.");
                    }

                    booking.ChangeSchedule(rescheduleEntity.ProposedStartTimeUtc, rescheduleEntity.ProposedEndTimeUtc);
                }
                else if (request.Status == RescheduleStatusAction.Reject)
                {
                    rescheduleEntity.Reject(currentUserId, request.ResponseNote);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return MapToResponse(rescheduleEntity);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static RescheduleResponse MapToResponse(RescheduleRequest entity)
        {
            return new RescheduleResponse(
                Id: entity.Id,
                BookingId: entity.BookingId,
                RequestedByUserId: entity.RequestedByUserId,
                OriginalStartTimeUtc: entity.OriginalStartTimeUtc,
                OriginalEndTimeUtc: entity.OriginalEndTimeUtc,
                ProposedStartTimeUtc: entity.ProposedStartTimeUtc,
                ProposedEndTimeUtc: entity.ProposedEndTimeUtc,
                Reason: entity.Reason,
                Status: entity.Status.ToString(),
                RespondedByUserId: entity.RespondedByUserId,
                ResponseNote: entity.ResponseNote,
                RequestedAtUtc: entity.RequestedAtUtc
            );
        }
    }
}