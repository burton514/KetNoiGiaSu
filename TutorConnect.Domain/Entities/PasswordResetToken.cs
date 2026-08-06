using TutorConnect.Domain.Common;

namespace TutorConnect.Domain.Entities
{
    /// <summary>
    /// Quản lý token đặt lại mật khẩu.
    /// Token có thời hạn 1 giờ và chỉ sử dụng được một lần.
    /// </summary>
    public sealed class PasswordResetToken : BaseEntity
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
        /// Thời điểm token được sử dụng để đặt lại mật khẩu; NULL nếu chưa dùng.
        /// </summary>
        public DateTime? UsedAtUtc { get; private set; }

        public User User { get; private set; } = null!;

        private PasswordResetToken()
        {
            // Dành cho EF Core.
        }

        private PasswordResetToken(long userId, string token, DateTime expiresAtUtc)
        {
            UserId = userId;
            Token = token;
            ExpiresAtUtc = expiresAtUtc;
        }

        /// <summary>
        /// Tạo token đặt lại mật khẩu mới. Token có hiệu lực trong 1 giờ.
        /// </summary>
        public static PasswordResetToken Create(long userId, string token)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("UserId không hợp lệ", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token không được để trống", nameof(token));
            }

            return new PasswordResetToken(
                userId: userId,
                token: token,
                expiresAtUtc: DateTime.UtcNow.AddHours(1));
        }

        /// <summary>
        /// Token có hiệu lực khi chưa hết hạn và chưa được sử dụng.
        /// </summary>
        public bool IsValid => ExpiresAtUtc > DateTime.UtcNow && UsedAtUtc is null;

        /// <summary>
        /// Token đã hết hạn.
        /// </summary>
        public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;

        /// <summary>
        /// Token đã được sử dụng.
        /// </summary>
        public bool IsUsed => UsedAtUtc is not null;

        /// <summary>
        /// Đánh dấu token là đã sử dụng.
        /// </summary>
        public void MarkAsUsed()
        {
            UsedAtUtc ??= DateTime.UtcNow;
        }
    }
}
