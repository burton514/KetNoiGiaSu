using TutorConnect.Domain.Entities;

namespace TutorConnect.Domain.Interfaces
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Booking>> GetByStudentIdAsync(long studentId, CancellationToken cancellationToken = default);
        Task<bool> IsTimeSlotConflictAsync(long tutorSubjectId, DateTime startTimeUtc, DateTime endTimeUtc, CancellationToken cancellationToken = default);
        Task AddAsync(Booking booking, CancellationToken cancellationToken = default);
        Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}