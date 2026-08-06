using TutorConnect.Domain.Common;

namespace TutorConnect.Domain.Entities
{
    public sealed class SessionProgress : BaseEntity
    {
        public long BookingId { get; private set; }
        public long LearningGoalId { get; private set; }
        public decimal? Score { get; private set; }
        public decimal? MaxScore { get; private set; }
        public decimal GoalProgressPercent { get; private set; }
        public string TutorComment { get; private set; } = string.Empty;

        public Booking? Booking { get; private set; }

        private SessionProgress() { }

        public static SessionProgress Create(long bookingId, long learningGoalId, decimal goalProgressPercent, string tutorComment, decimal? score = null, decimal? maxScore = null)
        {
            if (bookingId <= 0) throw new ArgumentException("BookingId không hợp lệ.");

            return new SessionProgress
            {
                BookingId = bookingId,
                LearningGoalId = learningGoalId,
                GoalProgressPercent = goalProgressPercent,
                TutorComment = tutorComment?.Trim() ?? string.Empty,
                Score = score,
                MaxScore = maxScore
            };
        }
    }
}