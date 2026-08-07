using TutorConnect.Application.Features.Reviews.DTOs;
using TutorConnect.Application.Features.Subjects.DTOs;
using TutorConnect.Application.Features.Users.DTOs;
using TutorConnect.Domain.Enums;

namespace TutorConnect.Application.Features.Bookings.DTOs
{
    public record BookingCreateRequest(
        long TutorSubjectId,
        DateTime StartTimeUtc,
        DateTime EndTimeUtc,
        string? StudentNote);

    public record ReasonRequest(
        string Reason);

    public record MeetingLinkRequest(
        string MeetingUrl);

    public record CompleteBookingRequest(
        long LearningGoalId,
        double? Score,
        double? MaxScore,
        double GoalProgressPercent,
        string TutorComment);

    public record BookingResponse(
        long Id,
        UserLiteResponse Student,
        UserLiteResponse Tutor,
        long? TutorSubjectId,
        SubjectResponse Subject,
        string TeachingLevel,
        DateTime StartTimeUtc,
        DateTime EndTimeUtc,
        int CreditCost,
        BookingStatus Status,
        string? StudentNote,
        string? MeetingUrl,
        string? StatusReason,
        long? CancelledByUserId,
        UserReputationSummaryResponse? StudentReputation,
        UserReputationSummaryResponse? TutorReputation);
}
