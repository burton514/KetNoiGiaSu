using System;
using TutorConnect.Domain.Enums;
using TutorConnect.Application.Features.Progress.DTOs;

namespace TutorConnect.Application.Features.Bookings.DTOs
{
    public record BookingCreateRequest(
        long TutorSubjectId,
        DateTime StartTimeUtc,
        DateTime EndTimeUtc,
        int CreditCost,
        string? StudentNote
    );

    public record BookingResponse(
        long Id,
        long StudentId,
        long TutorSubjectId,
        DateTime StartTimeUtc,
        DateTime EndTimeUtc,
        int CreditCost,
        BookingStatus Status,
        string? StudentNote
    );

    public record BookingMinimal(
        long Id,
        long StudentId,
        long TutorSubjectId,
        DateTime StartTimeUtc,
        DateTime EndTimeUtc,
        int CreditCost,
        BookingStatus Status,
        string? MeetingUrl
    );

    public record CompleteBookingResult(
        BookingMinimal Booking,
        SessionProgressResponse Progress
    );
}