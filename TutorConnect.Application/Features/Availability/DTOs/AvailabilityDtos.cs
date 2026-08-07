namespace TutorConnect.Application.Features.Availability.DTOs
{
    public record AvailabilityCreateRequest(
        DateTime StartTimeUtc,
        DateTime EndTimeUtc);

    public record AvailabilityUpdateRequest(
        DateTime StartTimeUtc,
        DateTime EndTimeUtc);

    public record TutorAvailabilityResponse(
        long Id,
        long TutorId,
        DateTime StartTimeUtc,
        DateTime EndTimeUtc);

    public record AvailableWindowResponse(
        DateTime StartTimeUtc,
        DateTime EndTimeUtc);
}
