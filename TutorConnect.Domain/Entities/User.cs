using TutorConnect.Domain.Common;
using TutorConnect.Domain.Enums;

namespace TutorConnect.Domain.Entities
{
    public class User : BaseEntity
    {
        private User()
        {
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

        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public string? Phone { get; private set; }
        public UserRole Role { get; private set; }
        public UserStatus Status { get; private set; }
        public string TimeZoneId { get; private set; } = string.Empty;

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

        public void UpdateProfile(string fullName, string? phone, string timeZoneId)
        {
            FullName = DomainGuard.Required(fullName, nameof(fullName), 150);
            Phone = DomainGuard.Optional(phone, nameof(phone), 30);
            TimeZoneId = DomainGuard.Required(timeZoneId, nameof(timeZoneId), 100);
        }

        public void ChangePasswordHash(string passwordHash)
        {
            PasswordHash = DomainGuard.Required(passwordHash, nameof(passwordHash), 500);
        }

        public void Activate()
        {
            Status = UserStatus.Active;
        }

        public void Lock()
        {
            Status = UserStatus.Locked;
        }

        public void Deactivate()
        {
            Status = UserStatus.Inactive;
        }
    }
}
