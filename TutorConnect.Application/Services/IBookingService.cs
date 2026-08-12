using System.Threading;
using System.Threading.Tasks;
using TutorConnect.Application.Features.Bookings.DTOs;

namespace TutorConnect.Application.Services
{
    public interface IBookingService
    {
        Task<BookingMinimal> CreateBookingAsync(BookingCreateRequest request, long studentId, CancellationToken cancellationToken = default);
        Task<RescheduleProposalDto> CreateRescheduleProposalAsync(long bookingId, long userId, RescheduleCreateRequest request, CancellationToken cancellationToken = default);
        Task<BookingMinimal> RespondToRescheduleProposalAsync(long bookingId, long proposalId, long userId, RescheduleStatusUpdateRequest request, CancellationToken cancellationToken = default);
    }
}