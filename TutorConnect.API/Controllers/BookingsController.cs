using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorConnect.Application.Services;
using TutorConnect.Application.Features.Progress.DTOs;
using TutorConnect.Application.Features.Bookings.DTOs;

namespace TutorConnect.API.Controllers
{
    [ApiController]
    [Route("api/v1/bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly IBookingService _bookingService;

        public BookingsController(ISessionService sessionService, IBookingService bookingService)
        {
            _sessionService = sessionService;
            _bookingService = bookingService;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<BookingMinimal>> CreateBooking([FromBody] BookingCreateRequest request, CancellationToken cancellationToken)
        {
            var studentId = GetCurrentUserId();
            if (studentId == 0) return Forbid();

            var result = await _bookingService.CreateBookingAsync(request, studentId, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{bookingId:long}/complete")]
        [Authorize(Roles = "Tutor")]
        public async Task<ActionResult<object>> CompleteBooking(long bookingId, [FromBody] SessionProgressUpsertRequest request, CancellationToken cancellationToken)
        {
            var tutorId = GetCurrentUserId();
            if (tutorId == 0) return Forbid();

            var result = await _sessionService.CompleteBookingAsync(bookingId, tutorId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{bookingId:long}/reschedule")]
        [Authorize]
        public async Task<ActionResult<RescheduleProposalDto>> CreateRescheduleProposal(long bookingId, [FromBody] RescheduleCreateRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Forbid();

            var result = await _bookingService.CreateRescheduleProposalAsync(bookingId, userId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{bookingId:long}/reschedule/{proposalId:long}/status")]
        [Authorize]
        public async Task<ActionResult<BookingMinimal>> RespondToRescheduleProposal(long bookingId, long proposalId, [FromBody] RescheduleStatusUpdateRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Forbid();

            var result = await _bookingService.RespondToRescheduleProposalAsync(bookingId, proposalId, userId, request, cancellationToken);
            return Ok(result);
        }
    }
}