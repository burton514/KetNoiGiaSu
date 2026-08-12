using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorConnect.Application.Services;
using TutorConnect.Application.Features.Progress.DTOs;

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

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<object>> CreateBooking([FromBody] TutorConnect.Application.Features.Bookings.DTOs.BookingCreateRequest request, CancellationToken cancellationToken)
        {
            // Extract student id from claims (assume claim "sub" or nameidentifier)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var studentId))
            {
                return Forbid();
            }

            var result = await _bookingService.CreateBookingAsync(request, studentId, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{bookingId:long}/complete")]
        [Authorize(Roles = "Tutor")]
        public async Task<ActionResult<object>> CompleteBooking(long bookingId, [FromBody] SessionProgressUpsertRequest request, CancellationToken cancellationToken)
        {
            var result = await _sessionService.CompleteBookingAsync(bookingId, request, cancellationToken);
            return Ok(result);
        }
    }
}
