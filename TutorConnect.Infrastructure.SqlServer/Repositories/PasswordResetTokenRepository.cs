using Microsoft.EntityFrameworkCore;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Interfaces;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public PasswordResetTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken?> GetByTokenAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            return await _context.PasswordResetTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
        }

        public async Task<PasswordResetToken?> GetLatestByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.PasswordResetTokens
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default)
        {
            await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
        }

        public async Task UpdateAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default)
        {
            _context.PasswordResetTokens.Update(token);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
