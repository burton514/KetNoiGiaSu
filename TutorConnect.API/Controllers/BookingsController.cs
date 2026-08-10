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

        public BookingsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
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
