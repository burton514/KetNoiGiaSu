using Microsoft.AspNetCore.Mvc;
using TutorConnect.Application.Services;
using TutorConnect.Application.Features.Bookings.DTOs;

namespace TutorConnect.API.Controllers
{
    [ApiController]
    [Route("api/v1/bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// 1. Tạo mới lịch học (Booking)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBooking(
            [FromBody] BookingCreateRequest request,
            CancellationToken cancellationToken)
        {
            long currentUserId = 1; // Giả lập UserId Học viên
            var result = await _bookingService.CreateBookingAsync(request, currentUserId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// 2. Đánh dấu hoàn thành buổi học
        /// </summary>
        [HttpPost("{bookingId:long}/complete")]
        public async Task<IActionResult> CompleteBooking(
            long bookingId,
            CancellationToken cancellationToken)
        {
            return Ok(new { Message = $"Booking {bookingId} marked as completed." });
        }

        /// <summary>
        /// 3. Gửi yêu cầu/đề xuất đổi lịch học (Reschedule)
        /// </summary>
        [HttpPost("{bookingId:long}/reschedule")]
        public async Task<IActionResult> RescheduleBooking(
            long bookingId,
            [FromBody] RescheduleCreateRequest request,
            CancellationToken cancellationToken)
        {
            long currentUserId = 1;

            var result = await _bookingService.CreateRescheduleRequestAsync(
                bookingId,
                currentUserId,
                request,
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// 4. Phê duyệt hoặc từ chối đề xuất đổi lịch
        /// </summary>
        [HttpPut("{bookingId:long}/reschedule/{proposalId:long}/status")]
        public async Task<IActionResult> UpdateRescheduleStatus(
            long bookingId,
            long proposalId,
            [FromBody] RescheduleStatusUpdateRequest request,
            CancellationToken cancellationToken)
        {
            long currentUserId = 2;

            var result = await _bookingService.RespondToRescheduleAsync(
                bookingId,
                proposalId,
                currentUserId,
                request,
                cancellationToken);

            return Ok(result);
        }
    }
}