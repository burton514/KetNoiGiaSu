using TutorConnect.Domain.Entities;

namespace TutorConnect.Domain.Interfaces
{
    /// <summary>
    /// Repository cho EmailVerificationToken entity.
    /// </summary>
    public interface IEmailVerificationTokenRepository
    {
        Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        Task<EmailVerificationToken?> GetLatestByUserIdAsync(long userId, CancellationToken cancellationToken = default);

        Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

        Task UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
