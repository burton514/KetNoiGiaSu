using Microsoft.EntityFrameworkCore;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Interfaces;
using TutorConnect.Infrastructure.SqlServer.Persistence;

namespace TutorConnect.Infrastructure.SqlServer.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
        }

        public async Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return null;
            }

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await _context.Users.AddAsync(user, cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return await _context.Users
                .AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
