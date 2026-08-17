using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorConnect.Application.Features.Bookings.DTOs;
using TutorConnect.Application.Services;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Enums;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Services
{
    public class BookingService : IBookingService
    {
        private const int MinimumNoticeHours = 12;

        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // KIỂM TRA TRÙNG LỊCH
        // =========================================================

        public async Task<bool> HasScheduleConflictAsync(
            long userId,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            long? excludeBookingId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    (
                        b.StudentId == userId ||
                        b.TutorSubject.Tutor.UserId == userId
                    )
                    &&
                    (
                        b.Status == BookingStatus.Pending ||
                        b.Status == BookingStatus.Confirmed
                    )
                    &&
                    startTimeUtc < b.EndTimeUtc &&
                    endTimeUtc > b.StartTimeUtc
                );

            if (excludeBookingId.HasValue)
            {
                query = query.Where(
                    b => b.Id != excludeBookingId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        // =========================================================
        // STUDENT TẠO BOOKING
        // =========================================================

        public async Task<BookingResponse> CreateBookingAsync(
            BookingCreateRequest request,
            long studentId,
            CancellationToken cancellationToken = default)
        {
            if (request.EndTimeUtc <= request.StartTimeUtc)
            {
                throw new InvalidOperationException(
                    "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
            }

            if (request.StartTimeUtc <=
                DateTime.UtcNow.AddHours(MinimumNoticeHours))
            {
                throw new InvalidOperationException(
                    "Phải đặt lịch trước thời gian bắt đầu ít nhất 12 tiếng.");
            }

            var tutorSubject = await _context.TutorSubjects
                .Include(ts => ts.Tutor)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(
                    ts => ts.Id == request.TutorSubjectId,
                    cancellationToken);

            if (tutorSubject == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy môn học của gia sư.");
            }

            if (!tutorSubject.IsActive)
            {
                throw new InvalidOperationException(
                    "Gia sư hiện không nhận môn học này.");
            }

            if (tutorSubject.Tutor == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy hồ sơ gia sư.");
            }

            if (tutorSubject.Tutor.ApprovalStatus !=
                TutorApprovalStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Gia sư chưa được Admin phê duyệt.");
            }

            if (tutorSubject.Tutor.User.Status !=
                UserStatus.Active)
            {
                throw new InvalidOperationException(
                    "Tài khoản gia sư không hoạt động.");
            }

            // Không tin CreditCost do Frontend gửi.
            // Lấy mức phí thật từ TutorSubject.
            if (request.CreditCost !=
                tutorSubject.FeePerSessionCredits)
            {
                throw new InvalidOperationException(
                    $"CreditCost không hợp lệ. Phí hiện tại là {tutorSubject.FeePerSessionCredits} credit.");
            }

            var tutorUserId =
                tutorSubject.Tutor.UserId;

            var studentConflict =
                await HasScheduleConflictAsync(
                    studentId,
                    request.StartTimeUtc,
                    request.EndTimeUtc,
                    null,
                    cancellationToken);

            var tutorConflict =
                await HasScheduleConflictAsync(
                    tutorUserId,
                    request.StartTimeUtc,
                    request.EndTimeUtc,
                    null,
                    cancellationToken);

            if (studentConflict || tutorConflict)
            {
                throw new InvalidOperationException(
                    "Lịch học bị trùng với lịch đã có.");
            }

            var booking = new Booking(
                studentId,
                request.TutorSubjectId,
                request.StartTimeUtc,
                request.EndTimeUtc,
                tutorSubject.FeePerSessionCredits,
                request.StudentNote
            );

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync(
                cancellationToken);

            return MapToResponse(booking);
        }

        // =========================================================
        // LẤY DANH SÁCH BOOKING
        // =========================================================

        public async Task<IEnumerable<BookingResponse>>
            GetUserBookingsAsync(
                long userId,
                string? status = null,
                CancellationToken cancellationToken = default)
        {
            var query = _context.Bookings
                .Include(b => b.TutorSubject)
                    .ThenInclude(ts => ts.Tutor)
                .AsNoTracking()
                .Where(b =>
                    b.StudentId == userId ||
                    b.TutorSubject.Tutor.UserId == userId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<BookingStatus>(
                        status,
                        true,
                        out var bookingStatus))
                {
                    throw new InvalidOperationException(
                        "Trạng thái Booking không hợp lệ.");
                }

                query = query.Where(
                    b => b.Status == bookingStatus);
            }

            var list = await query
                .OrderByDescending(
                    b => b.StartTimeUtc)
                .ToListAsync(cancellationToken);

            return list.Select(MapToResponse);
        }

        // =========================================================
        // CHI TIẾT BOOKING
        // =========================================================

        public async Task<BookingResponse?>
            GetBookingByIdAsync(
                long bookingId,
                long userId,
                CancellationToken cancellationToken = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.TutorSubject)
                    .ThenInclude(ts => ts.Tutor)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == bookingId &&
                        (
                            b.StudentId == userId ||
                            b.TutorSubject.Tutor.UserId == userId
                        ),
                    cancellationToken);

            return booking == null
                ? null
                : MapToResponse(booking);
        }

        // =========================================================
        // TUTOR CONFIRM
        // =========================================================

        public async Task<bool> ConfirmBookingAsync(
            long bookingId,
            long tutorUserId,
            string? meetingUrl,
            CancellationToken cancellationToken = default)
        {
            var booking =
                await GetBookingForTutorAsync(
                    bookingId,
                    tutorUserId,
                    cancellationToken);

            if (booking == null)
            {
                return false;
            }

            if (booking.Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Chỉ Booking Pending mới được xác nhận.");
            }

            var studentConflict =
                await HasScheduleConflictAsync(
                    booking.StudentId,
                    booking.StartTimeUtc,
                    booking.EndTimeUtc,
                    booking.Id,
                    cancellationToken);

            var tutorConflict =
                await HasScheduleConflictAsync(
                    tutorUserId,
                    booking.StartTimeUtc,
                    booking.EndTimeUtc,
                    booking.Id,
                    cancellationToken);

            if (studentConflict || tutorConflict)
            {
                throw new InvalidOperationException(
                    "Booking không thể xác nhận vì lịch đã bị trùng.");
            }

            booking.Confirm(meetingUrl);

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // TUTOR REJECT
        // =========================================================

        public async Task<bool> RejectBookingAsync(
            long bookingId,
            long tutorUserId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var booking =
                await GetBookingForTutorAsync(
                    bookingId,
                    tutorUserId,
                    cancellationToken);

            if (booking == null)
            {
                return false;
            }

            booking.Reject(reason);

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // CANCEL
        // =========================================================

        public async Task<bool> CancelBookingAsync(
            long bookingId,
            long userId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.TutorSubject)
                    .ThenInclude(ts => ts.Tutor)
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == bookingId &&
                        (
                            b.StudentId == userId ||
                            b.TutorSubject.Tutor.UserId == userId
                        ),
                    cancellationToken);

            if (booking == null)
            {
                return false;
            }

            if (booking.StartTimeUtc <=
                DateTime.UtcNow.AddHours(MinimumNoticeHours))
            {
                throw new InvalidOperationException(
                    "Chỉ được hủy lịch trước thời gian bắt đầu ít nhất 12 tiếng.");
            }

            booking.Cancel(userId, reason);

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // UPDATE MEETING URL
        // =========================================================

        public async Task<bool> UpdateMeetingUrlAsync(
            long bookingId,
            long tutorUserId,
            string meetingUrl,
            CancellationToken cancellationToken = default)
        {
            var booking =
                await GetBookingForTutorAsync(
                    bookingId,
                    tutorUserId,
                    cancellationToken);

            if (booking == null)
            {
                return false;
            }

            booking.SetMeetingUrl(meetingUrl);

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // TẠO ĐỀ XUẤT ĐỔI LỊCH
        // =========================================================

        public async Task<RescheduleResponse>
            CreateRescheduleRequestAsync(
                long bookingId,
                long currentUserId,
                RescheduleCreateRequest request,
                CancellationToken cancellationToken = default)
        {
            if (request.ProposedEndTimeUtc <=
                request.ProposedStartTimeUtc)
            {
                throw new InvalidOperationException(
                    "Thời gian kết thúc mới phải lớn hơn thời gian bắt đầu.");
            }

            if (request.ProposedStartTimeUtc <=
                DateTime.UtcNow.AddHours(MinimumNoticeHours))
            {
                throw new InvalidOperationException(
                    "Thời gian mới phải cách hiện tại ít nhất 12 tiếng.");
            }

            var booking = await _context.Bookings
                .Include(b => b.TutorSubject)
                    .ThenInclude(ts => ts.Tutor)
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == bookingId &&
                        (
                            b.StudentId == currentUserId ||
                            b.TutorSubject.Tutor.UserId ==
                                currentUserId
                        ),
                    cancellationToken);

            if (booking == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy Booking hoặc bạn không có quyền.");
            }

            if (booking.Status != BookingStatus.Pending &&
                booking.Status != BookingStatus.Confirmed)
            {
                throw new InvalidOperationException(
                    "Chỉ Booking Pending hoặc Confirmed mới được đổi lịch.");
            }

            var pendingExists =
                await _context.RescheduleRequests
                    .AnyAsync(
                        r =>
                            r.BookingId == bookingId &&
                            r.Status ==
                                RescheduleRequestStatus.Pending,
                        cancellationToken);

            if (pendingExists)
            {
                throw new InvalidOperationException(
                    "Booking đang có một đề xuất đổi lịch chờ xử lý.");
            }

            var conflict =
                await HasScheduleConflictAsync(
                    currentUserId,
                    request.ProposedStartTimeUtc,
                    request.ProposedEndTimeUtc,
                    bookingId,
                    cancellationToken);

            if (conflict)
            {
                throw new InvalidOperationException(
                    "Thời gian mới bị trùng với lịch khác.");
            }

            var proposal = new RescheduleRequest(
                bookingId,
                currentUserId,
                booking.StartTimeUtc,
                booking.EndTimeUtc,
                request.ProposedStartTimeUtc,
                request.ProposedEndTimeUtc,
                DateTime.UtcNow,
                request.Reason
            );

            _context.RescheduleRequests.Add(
                proposal);

            await _context.SaveChangesAsync(
                cancellationToken);

            return MapToRescheduleResponse(
                proposal);
        }

        // =========================================================
        // APPROVE / REJECT ĐỔI LỊCH
        // =========================================================

        public async Task<RescheduleResponse>
            RespondToRescheduleAsync(
                long bookingId,
                long proposalId,
                long currentUserId,
                RescheduleStatusUpdateRequest request,
                CancellationToken cancellationToken = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.TutorSubject)
                    .ThenInclude(ts => ts.Tutor)
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == bookingId &&
                        (
                            b.StudentId == currentUserId ||
                            b.TutorSubject.Tutor.UserId ==
                                currentUserId
                        ),
                    cancellationToken);

            if (booking == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy Booking hoặc bạn không có quyền.");
            }

            var proposal =
                await _context.RescheduleRequests
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id == proposalId &&
                            p.BookingId == bookingId,
                        cancellationToken);

            if (proposal == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy đề xuất đổi lịch.");
            }

            if (proposal.Status !=
                RescheduleRequestStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Đề xuất đổi lịch đã được xử lý.");
            }

            if (proposal.RequestedByUserId ==
                currentUserId)
            {
                throw new InvalidOperationException(
                    "Người tạo đề xuất không thể tự phê duyệt đề xuất.");
            }

            if (request.Status ==
                RescheduleStatusAction.Approve)
            {
                var studentConflict =
                    await HasScheduleConflictAsync(
                        booking.StudentId,
                        proposal.ProposedStartTimeUtc,
                        proposal.ProposedEndTimeUtc,
                        booking.Id,
                        cancellationToken);

                var tutorUserId =
                    booking.TutorSubject.Tutor.UserId;

                var tutorConflict =
                    await HasScheduleConflictAsync(
                        tutorUserId,
                        proposal.ProposedStartTimeUtc,
                        proposal.ProposedEndTimeUtc,
                        booking.Id,
                        cancellationToken);

                if (studentConflict || tutorConflict)
                {
                    throw new InvalidOperationException(
                        "Thời gian mới bị trùng với lịch hiện tại.");
                }

                proposal.Accept(
                    currentUserId,
                    request.ResponseNote);

                booking.ChangeSchedule(
                    proposal.ProposedStartTimeUtc,
                    proposal.ProposedEndTimeUtc);
            }
            else
            {
                proposal.Reject(
                    currentUserId,
                    request.ResponseNote);
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            return MapToRescheduleResponse(
                proposal);
        }

        // =========================================================
        // COMPLETE BOOKING
        // =========================================================

        public async Task<bool> CompleteBookingAsync(
            long bookingId,
            long currentUserId,
            CompleteBookingRequest? request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new InvalidOperationException(
                    "Thông tin kết quả buổi học là bắt buộc.");
            }

            var booking = await _context.Bookings
                .Include(b => b.SessionProgress)
                .Include(b => b.TutorSubject)
                    .ThenInclude(ts => ts.Tutor)
                        .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == bookingId &&
                        b.TutorSubject.Tutor.UserId ==
                            currentUserId,
                    cancellationToken);

            if (booking == null)
            {
                return false;
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                throw new InvalidOperationException(
                    "Chỉ Booking Confirmed mới được hoàn thành.");
            }

            if (DateTime.UtcNow < booking.EndTimeUtc)
            {
                throw new InvalidOperationException(
                    "Chưa thể hoàn thành buổi học trước thời gian kết thúc.");
            }

            if (booking.SessionProgress != null)
            {
                throw new InvalidOperationException(
                    "Booking này đã được ghi nhận kết quả.");
            }

            var learningGoal =
                await _context.LearningGoals
                    .FirstOrDefaultAsync(
                        g =>
                            g.Id == request.LearningGoalId &&
                            g.StudentId == booking.StudentId &&
                            g.TutorSubjectId ==
                                booking.TutorSubjectId,
                        cancellationToken);

            if (learningGoal == null)
            {
                throw new InvalidOperationException(
                    "LearningGoal không tồn tại hoặc không thuộc Booking này.");
            }

            var progress = new SessionProgress(
                bookingId,
                request.LearningGoalId,
                (decimal?)request.Score,
                (decimal?)request.MaxScore,
                (decimal)request.GoalProgressPercent,
                request.TutorComment
            );

            _context.SessionProgress.Add(
                progress);

            booking.Complete();

            learningGoal.SynchronizeStatus(
                (decimal)request.GoalProgressPercent);

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =========================================================
        // HELPER
        // =========================================================

        private async Task<Booking?>
            GetBookingForTutorAsync(
                long bookingId,
                long tutorUserId,
                CancellationToken cancellationToken)
        {
            return await _context.Bookings
                .Include(b => b.TutorSubject)
                    .ThenInclude(ts => ts.Tutor)
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == bookingId &&
                        b.TutorSubject.Tutor.UserId ==
                            tutorUserId,
                    cancellationToken);
        }

        private static BookingResponse
            MapToResponse(Booking booking)
        {
            return new BookingResponse(
                booking.Id,
                booking.StudentId,
                booking.TutorSubjectId,
                booking.StartTimeUtc,
                booking.EndTimeUtc,
                booking.CreditCost,
                booking.Status,
                booking.StudentNote,
                booking.MeetingUrl,
                booking.StatusReason,
                booking.CancelledByUserId
            );
        }

        private static RescheduleResponse
            MapToRescheduleResponse(
                RescheduleRequest request)
        {
            return new RescheduleResponse(
                request.Id,
                request.BookingId,
                request.RequestedByUserId,
                request.OriginalStartTimeUtc,
                request.OriginalEndTimeUtc,
                request.ProposedStartTimeUtc,
                request.ProposedEndTimeUtc,
                request.Reason,
                request.Status.ToString(),
                request.RespondedByUserId,
                request.ResponseNote,
                request.RequestedAtUtc
            );
        }
    }
}