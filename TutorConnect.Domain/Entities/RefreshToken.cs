using TutorConnect.Domain.Common;

namespace TutorConnect.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        private RefreshToken()
        {
        }

        public RefreshToken(long userId, string tokenHash, DateTime expiresAtUtc)
        {
            DomainGuard.Positive(userId, nameof(userId));
            UserId = userId;
            TokenHash = DomainGuard.Required(tokenHash, nameof(tokenHash), 64);

            if (TokenHash.Length != 64)
            {
                throw new ArgumentException("TokenHash must contain exactly 64 characters.", nameof(tokenHash));
            }

            ExpiresAtUtc = expiresAtUtc;
        }

        public long UserId { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; private set; }
        public DateTime? RevokedAtUtc { get; private set; }

        public User User { get; private set; } = null!;

        public bool IsActive(DateTime utcNow)
        {
            return RevokedAtUtc is null && ExpiresAtUtc > utcNow;
        }

        public void Revoke(DateTime revokedAtUtc)
        {
            if (RevokedAtUtc is null)
            {
                RevokedAtUtc = revokedAtUtc;
            }
        }
    }
}
