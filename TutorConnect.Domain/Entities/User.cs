using TutorConnect.Domain.Common;
using TutorConnect.Domain.Enums;

namespace TutorConnect.Domain.Entities
{
    /// <summary>
    /// Tài khoản dùng chung cho ba vai trò Student, Tutor và Admin.
    /// Là nguồn định danh và phân quyền trung tâm của hệ thống.
    /// </summary>
    public sealed class User : BaseEntity
    {
        public string Email { get; private set; } = null!;

        /// <summary>
        /// Mật khẩu đã băm (BCrypt)
        /// </summary>
        public string PasswordHash { get; private set; } = null!;

        public string FullName { get; private set; } = null!;

        public string? Phone { get; private set; }

        /// <summary>
        /// Vai trò duy nhất của tài khoản; cố định sau khi tạo..
        /// </summary>
        public UserRole Role { get; private set; }

        public UserStatus Status { get; private set; }

        /// <summary>
        /// Thời điểm email được xác minh; NULL nếu chưa xác minh. Đây là khái niệm
        /// tách biệt hoàn toàn với <see cref="Status"/> (khóa/mở tài khoản) - xác
        /// minh email KHÔNG được phép tự động mở khóa một tài khoản đang bị Locked.
        /// </summary>
        public DateTime? EmailVerifiedAtUtc { get; private set; }

        public bool IsEmailVerified => EmailVerifiedAtUtc is not null;

        /// <summary>
        /// Múi giờ hiển thị, vd Asia/Ho_Chi_Minh. Các mốc thời gian nghiệp vụ
        /// khác trong hệ thống luôn lưu UTC và quy đổi dựa trên trường này.
        /// </summary>
        public string TimeZoneId { get; private set; } = null!;

        public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

        private User()
        {
            // Dành cho EF Core.
        }

        private User(
            string email,
            string passwordHash,
            string fullName,
            string? phone,
            UserRole role,
            string timeZoneId)
        {
            Email = email;
            PasswordHash = passwordHash;
            FullName = fullName;
            Phone = phone;
            Role = role;
            Status = UserStatus.Active;
            TimeZoneId = timeZoneId;
        }

        /// <summary>
        /// Tạo tài khoản mới. PasswordHash phải được băm sẵn ở Application layer
        /// (qua IPasswordHasher) trước khi truyền vào đây - Domain không tự hash.
        /// </summary>
        public static User Create(
            string email,
            string passwordHash,
            string fullName,
            UserRole role,
            string timeZoneId,
            string? phone = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email không được để trống.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("PasswordHash không được để trống.", nameof(passwordHash));
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("FullName không được để trống.", nameof(fullName));
            }

            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                throw new ArgumentException("TimeZoneId không được để trống.", nameof(timeZoneId));
            }

            return new User(
                email: email.Trim().ToLowerInvariant(),
                passwordHash: passwordHash,
                fullName: fullName.Trim(),
                phone: string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                role: role,
                timeZoneId: timeZoneId.Trim());
        }

        /// <summary>
        /// Tài khoản Locked không được đăng nhập hoặc thực hiện nghiệp vụ mới.
        /// Việc email đã xác minh hay chưa được kiểm tra riêng qua <see cref="IsEmailVerified"/>,
        /// không trộn vào Status để tránh lỗ hổng "xác minh email tự mở khóa tài khoản".
        /// </summary>
        public bool CanSignIn => Status == UserStatus.Active;

        public void ChangePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ArgumentException("PasswordHash không được để trống.", nameof(newPasswordHash));
            }

            PasswordHash = newPasswordHash;
        }

        /// <summary>
        /// Cập nhật mật khẩu và vô hiệu hóa tất cả các refresh token hiện tại (buộc đăng nhập lại).
        /// </summary>
        public void UpdatePassword(string newPasswordHash)
        {
            ChangePassword(newPasswordHash);
            // Vô hiệu hóa tất cả refresh tokens để buộc đăng nhập lại tất cả các thiết bị
            foreach (var token in RefreshTokens.Where(t => !t.IsRevoked))
            {
                token.Revoke();
            }
        }

        public void UpdateProfile(string fullName, string? phone, string timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("FullName không được để trống.", nameof(fullName));
            }

            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                throw new ArgumentException("TimeZoneId không được để trống.", nameof(timeZoneId));
            }

            FullName = fullName.Trim();
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            TimeZoneId = timeZoneId.Trim();
        }

        public void Lock() => Status = UserStatus.Locked;

        public void Activate() => Status = UserStatus.Active;

        public void Deactivate() => Status = UserStatus.Inactive;

        /// <summary>
        /// Đánh dấu email đã được xác minh. KHÔNG thay đổi <see cref="Status"/> -
        /// nếu tài khoản đang Locked, nó vẫn Locked sau khi xác minh email thành công.
        /// </summary>
        public void MarkEmailVerified()
        {
            EmailVerifiedAtUtc ??= DateTime.UtcNow;
        }
    }
}
