namespace TutorConnect.Application.Features.Auth.DTOs
{
    /// <summary>
    /// Request để gửi lại email xác minh.
    /// </summary>
    public record ResendVerificationEmailRequest(
        string Email);

    /// <summary>
    /// Request để xác minh email.
    /// </summary>
    public record VerifyEmailRequest(
        string Token);

    /// <summary>
    /// Response sau khi xác minh email thành công.
    /// </summary>
    public record VerifyEmailResponse(
        bool Success,
        string Message);
}
