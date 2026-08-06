using TutorConnect.Domain.Common;

namespace TutorConnect.Domain.Entities
{
    public class TutorAvailability : BaseEntity
    {
        private TutorAvailability()
        {
        }

        public TutorAvailability(long tutorId, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            DomainGuard.Positive(tutorId, nameof(tutorId));
            DomainGuard.Period(startTimeUtc, endTimeUtc);
            TutorId = tutorId;
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
        }

        public long TutorId { get; private set; }
        public DateTime StartTimeUtc { get; private set; }
        public DateTime EndTimeUtc { get; private set; }

        public TutorProfile Tutor { get; private set; } = null!;

        public void ChangePeriod(DateTime startTimeUtc, DateTime endTimeUtc)
        {
            DomainGuard.Period(startTimeUtc, endTimeUtc);
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
        }
    }
}
