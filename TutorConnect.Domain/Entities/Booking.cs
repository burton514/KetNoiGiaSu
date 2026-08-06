using TutorConnect.Domain.Common;
using TutorConnect.Domain.Enums;

namespace TutorConnect.Domain.Entities
{
    public sealed class Booking : BaseEntity
    {
        public long StudentId { get; private set; }
        public long TutorSubjectId { get; private set; }
        public DateTime StartTimeUtc { get; private set; }
        public DateTime EndTimeUtc { get; private set; }
        public int CreditCost { get; private set; }
        public BookingStatus Status { get; private set; }
        public string? StudentNote { get; private set; }
        public string? MeetingUrl { get; private set; }
        public string? StatusReason { get; private set; }
        public long? CancelledByUserId { get; private set; }

        public SessionProgress? SessionProgress { get; private set; }
        public ICollection<Review> Reviews { get; private set; } = new List<Review>();

        private Booking() { }

        private Booking(long studentId, long tutorSubjectId, DateTime startTimeUtc, DateTime endTimeUtc, int creditCost, string? studentNote)
        {
            StudentId = studentId;
            TutorSubjectId = tutorSubjectId;
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
            CreditCost = creditCost;
            Status = BookingStatus.Pending;
            StudentNote = studentNote;
        }

        public static Booking Create(long studentId, long tutorSubjectId, DateTime startTimeUtc, DateTime endTimeUtc, int creditCost, string? studentNote = null)
        {
            if (studentId <= 0) throw new ArgumentException("StudentId không hợp lệ.", nameof(studentId));
            if (tutorSubjectId <= 0) throw new ArgumentException("TutorSubjectId không hợp lệ.", nameof(tutorSubjectId));
            if (creditCost <= 0) throw new ArgumentException("CreditCost phải lớn hơn 0.", nameof(creditCost));
            if (startTimeUtc >= endTimeUtc) throw new ArgumentException("Thời gian không hợp lệ.");

            return new Booking(studentId, tutorSubjectId, startTimeUtc, endTimeUtc, creditCost, string.IsNullOrWhiteSpace(studentNote) ? null : studentNote.Trim());
        }

        public void UpdateStatus(BookingStatus newStatus, string? reason = null)
        {
            Status = newStatus;
            StatusReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            // Code cập nhật thời gian tùy thuộc vào BaseEntity của hệ thống
        }
    }
}