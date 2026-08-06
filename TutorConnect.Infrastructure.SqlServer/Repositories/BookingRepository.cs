using Microsoft.EntityFrameworkCore;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Enums;
using TutorConnect.Domain.Interfaces;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return null;
            return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Booking>> GetByStudentIdAsync(long studentId, CancellationToken cancellationToken = default)
        {
            if (studentId <= 0) return Enumerable.Empty<Booking>();
            return await _context.Bookings
                .AsNoTracking()
                .Where(b => b.StudentId == studentId)
                .OrderByDescending(b => b.StartTimeUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsTimeSlotConflictAsync(long tutorSubjectId, DateTime startTimeUtc, DateTime endTimeUtc, CancellationToken cancellationToken = default)
        {
            if (tutorSubjectId <= 0) return false;

            return await _context.Bookings
                .AsNoTracking()
                .Where(b => b.TutorSubjectId == tutorSubjectId
                         && b.Status != BookingStatus.Cancelled
                         && b.Status != BookingStatus.Rejected)
                .AnyAsync(b => startTimeUtc < b.EndTimeUtc && endTimeUtc > b.StartTimeUtc, cancellationToken);
        }

        public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            if (booking == null) throw new ArgumentNullException(nameof(booking));
            await _context.Bookings.AddAsync(booking, cancellationToken);
        }

        public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            if (booking == null) throw new ArgumentNullException(nameof(booking));
            _context.Bookings.Update(booking);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}