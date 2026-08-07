using Microsoft.EntityFrameworkCore;
using TutorConnect.Domain.Common;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Interfaces;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Repositories
{
    public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public EmailVerificationTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EmailVerificationToken?> GetByTokenAsync(string rawToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return null;
            }

            var tokenHash = TokenHasher.Hash(rawToken);

            return await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<EmailVerificationToken?> GetLatestByUserIdAsync(long userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return null;
            }

            return await _context.EmailVerificationTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
        }

        public async Task UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            _context.EmailVerificationTokens.Update(token);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
