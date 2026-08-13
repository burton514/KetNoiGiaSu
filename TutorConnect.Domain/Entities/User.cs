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
        public TutorProfile? TutorProfile { get; private set; }
        public ICollection<TutorProfile> ReviewedTutorProfiles { get; private set; } = new List<TutorProfile>();
        public ICollection<Booking> StudentBookings { get; private set; } = new List<Booking>();
        public ICollection<Booking> CancelledBookings { get; private set; } = new List<Booking>();
        public ICollection<RescheduleRequest> RequestedRescheduleRequests { get; private set; } = new List<RescheduleRequest>();
        public ICollection<RescheduleRequest> RespondedRescheduleRequests { get; private set; } = new List<RescheduleRequest>();
        public ICollection<LearningGoal> LearningGoals { get; private set; } = new List<LearningGoal>();
        public ICollection<Review> Reviews { get; private set; } = new List<Review>();
        public ICollection<Complaint> CreatedComplaints { get; private set; } = new List<Complaint>();
        public ICollection<Complaint> ComplaintsAgainstUser { get; private set; } = new List<Complaint>();
        public ICollection<Complaint> ResolvedComplaints { get; private set; } = new List<Complaint>();

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

        public User(
            string email,
            string passwordHash,
            string fullName,
            UserRole role,
            string timeZoneId,
            string? phone = null,
            UserStatus status = UserStatus.Active)
        {
            Email = DomainGuard.Email(email, nameof(email));
            PasswordHash = DomainGuard.Required(passwordHash, nameof(passwordHash), 500);
            FullName = DomainGuard.Required(fullName, nameof(fullName), 150);
            Phone = DomainGuard.Optional(phone, nameof(phone), 30);
            DomainGuard.DefinedEnum(role, nameof(role));
            DomainGuard.DefinedEnum(status, nameof(status));
            Role = role;
            Status = status;
            TimeZoneId = DomainGuard.Required(timeZoneId, nameof(timeZoneId), 100);
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
            DomainGuard.DefinedEnum(role, nameof(role));

            return new User(
                email: DomainGuard.Email(email, nameof(email)),
                passwordHash: DomainGuard.Required(passwordHash, nameof(passwordHash), 500),
                fullName: DomainGuard.Required(fullName, nameof(fullName), 150),
                phone: DomainGuard.Optional(phone, nameof(phone), 30),
                role: role,
                timeZoneId: DomainGuard.Required(timeZoneId, nameof(timeZoneId), 100));
        }

        public TutorProfile InitializeTutorProfile()
        {
            if (Role != UserRole.Tutor)
            {
                throw new InvalidOperationException("Only Tutor users can have a TutorProfile.");
            }

            if (TutorProfile is not null)
            {
                throw new InvalidOperationException("TutorProfile has already been initialized.");
            }

            TutorProfile = new TutorProfile(this);
            return TutorProfile;
        }

        public bool CanSignIn => Status == UserStatus.Active;

        public void ChangePassword(string newPasswordHash)
        {
            PasswordHash = DomainGuard.Required(newPasswordHash, nameof(newPasswordHash), 500);
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
            FullName = DomainGuard.Required(fullName, nameof(fullName), 150);
            Phone = DomainGuard.Optional(phone, nameof(phone), 30);
            TimeZoneId = DomainGuard.Required(timeZoneId, nameof(timeZoneId), 100);
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
