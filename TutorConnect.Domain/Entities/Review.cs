using TutorConnect.Domain.Common;

namespace TutorConnect.Domain.Entities
{
    public sealed class Review : BaseEntity
    {
        public long BookingId { get; private set; }
        public long ReviewerId { get; private set; }
        public byte Rating { get; private set; }
        public string? Comment { get; private set; }

        public Booking? Booking { get; private set; }

        private Review() { }

        public static Review Create(long bookingId, long reviewerId, byte rating, string? comment)
        {
            if (bookingId <= 0) throw new ArgumentException("BookingId không hợp lệ.");
            if (rating < 1 || rating > 5) throw new ArgumentException("Đánh giá phải từ 1 đến 5.");

            return new Review
            {
                BookingId = bookingId,
                ReviewerId = reviewerId,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
            };
        }
    }
}