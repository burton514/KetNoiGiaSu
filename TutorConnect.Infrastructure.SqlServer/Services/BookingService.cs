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

        public async Task<BookingMinimal> CreateBookingAsync(BookingCreateRequest request, long studentId, CancellationToken cancellationToken = default)
        {
            // Validate tutor subject exists
            var tutorSubject = await _dbContext.Set<TutorSubject>()
                .Include(ts => ts.Tutor)
                .FirstOrDefaultAsync(ts => ts.Id == request.TutorSubjectId, cancellationToken);

            if (tutorSubject == null)
                throw new KeyNotFoundException("TutorSubject not found");

            // Check conflicts (existing Pending/Confirmed bookings for tutor or student)
            var hasConflict = await _dbContext.Set<Booking>()
                .AnyAsync(b => (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                    && ((b.TutorSubjectId == request.TutorSubjectId) || b.StudentId == studentId)
                    && (request.StartTimeUtc < b.EndTimeUtc && request.EndTimeUtc > b.StartTimeUtc), cancellationToken);

            if (hasConflict)
                throw new InvalidOperationException("Booking time conflict");

            // Snapshot credit cost from TutorSubject if any (assume fee per session present)
            var creditCost = (int?)null;
            // If TutorSubject has fee, use it
            try { creditCost = tutorSubject.FeePerSessionCredits; } catch { creditCost = 1; }

            var booking = new Booking(studentId, request.TutorSubjectId, request.StartTimeUtc, request.EndTimeUtc, creditCost ?? 1, request.StudentNote);

            await _dbContext.Set<Booking>().AddAsync(booking, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Return minimal response
            return new BookingMinimal(booking.Id, booking.StudentId, booking.TutorSubjectId, booking.StartTimeUtc, booking.EndTimeUtc, booking.CreditCost, booking.Status, booking.MeetingUrl);
        }
    }
}
