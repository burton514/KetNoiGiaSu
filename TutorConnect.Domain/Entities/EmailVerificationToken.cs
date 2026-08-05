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
        /// Mã token được gửi qua email (base64 encoded random bytes).
        /// </summary>
        public string Token { get; private set; } = null!;

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

        private EmailVerificationToken(long userId, string token, DateTime expiresAtUtc)
        {
            UserId = userId;
            Token = token;
            ExpiresAtUtc = expiresAtUtc;
        }

        /// <summary>
        /// Tạo token xác minh email mới. Token có hiệu lực trong 24 giờ.
        /// </summary>
        public static EmailVerificationToken Create(long userId, string token)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("UserId không hợp lệ", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token không được để trống", nameof(token));
            }

            return new EmailVerificationToken(
                userId: userId,
                token: token,
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
