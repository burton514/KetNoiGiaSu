using TutorConnect.Domain.Enums;

namespace TutorConnect.Application.Features.Users.DTOs
{
    public record IdNameResponse(
        long Id,
        string Name);

    public record UserLiteResponse(
        long Id,
        string FullName,
        UserRole Role);

    public record UserProfileResponse(
        long Id,
        string Email,
        string FullName,
        string? Phone,
        UserRole Role,
        UserStatus Status,
        string TimeZoneId);

    public record UpdateMeRequest(
        string FullName,
        string? Phone,
        string TimeZoneId);

    public record UpdateUserStatusRequest(
        UserStatus Status,
        string? Reason);
}
