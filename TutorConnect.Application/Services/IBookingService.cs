using System.Threading;
using System.Threading.Tasks;
using TutorConnect.Application.Features.Bookings.DTOs;

namespace TutorConnect.Application.Services
{
    public interface IBookingService
    {
        Task<BookingMinimal> CreateBookingAsync(BookingCreateRequest request, long studentId, CancellationToken cancellationToken = default);
    }
}
