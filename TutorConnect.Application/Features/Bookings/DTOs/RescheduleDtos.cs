using System;

namespace TutorConnect.Application.Features.Bookings.DTOs
{
    public record RescheduleCreateRequest(
        DateTime NewStartTimeUtc,
        DateTime NewEndTimeUtc,
        string? Reason
    );

    public record RescheduleStatusUpdateRequest(
        bool IsApproved,
        string? Note
    );

    public record RescheduleProposalDto(
        long Id,
        long BookingId,
        long RequestedByUserId,
        DateTime NewStartTimeUtc,
        DateTime NewEndTimeUtc,
        string Status,
        string? Reason
    );
}