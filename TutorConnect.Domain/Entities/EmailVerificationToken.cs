using TutorConnect.Domain.Common;

namespace TutorConnect.Domain.Entities
{
    /// <summary>
    /// Quản lý token xác minh email.
    /// Một User có thể có nhiều EmailVerificationToken (nếu user yêu cầu gửi lại).
    /// Token chỉ hoạt động cho đến khi hết hạn hoặc được xác minh thành công.
    /// </summary>
    public sealed class EmailVerificationToken : BaseEntity
    {
        public long UserId { get; private set; }

        /// <summary>
        /// Bản băm SHA-256 (hex, 64 ký tự) của token gửi qua email. Không bao giờ
        /// lưu token thô - chỉ lưu hash để tránh lộ token nếu Database bị rò rỉ.
        /// </summary>
        public string TokenHash { get; private set; } = null!;

        /// <summary>
        /// Thời điểm token hết hạn (UTC).
        /// </summary>
        public DateTime ExpiresAtUtc { get; private set; }

        /// <summary>
        /// Thời điểm token được xác minh; NULL nếu chưa xác minh.
        /// </summary>
        public DateTime? VerifiedAtUtc { get; private set; }

        public User User { get; private set; } = null!;

        private EmailVerificationToken()
        {
            // Dành cho EF Core.
        }

        private EmailVerificationToken(long userId, string tokenHash, DateTime expiresAtUtc)
        {
            UserId = userId;
            TokenHash = tokenHash;
            ExpiresAtUtc = expiresAtUtc;
        }

        /// <summary>
        /// Tạo token xác minh email mới từ token thô (raw token sẽ được gửi qua email,
        /// chỉ bản hash của nó được lưu vào Database). Token có hiệu lực trong 24 giờ.
        /// </summary>
        public static EmailVerificationToken Create(long userId, string rawToken)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("UserId không hợp lệ", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(rawToken))
            {
                throw new ArgumentException("Token không được để trống", nameof(rawToken));
            }

            return new EmailVerificationToken(
                userId: userId,
                tokenHash: TokenHasher.Hash(rawToken),
                expiresAtUtc: DateTime.UtcNow.AddHours(24));
        }

        /// <summary>
        /// Token có hiệu lực khi chưa hết hạn và chưa được xác minh.
        /// </summary>
        public bool IsValid => ExpiresAtUtc > DateTime.UtcNow && VerifiedAtUtc is null;

        /// <summary>
        /// Token đã hết hạn.
        /// </summary>
        public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;

        /// <summary>
        /// Đánh dấu token là đã xác minh.
        /// </summary>
        public void MarkAsVerified()
        {
            VerifiedAtUtc ??= DateTime.UtcNow;
        }
    }
}
