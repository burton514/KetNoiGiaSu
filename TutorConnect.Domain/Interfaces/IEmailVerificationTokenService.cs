namespace TutorConnect.Domain.Interfaces
{
    /// <summary>
    /// Dịch vụ sinh tạo token xác minh email.
    /// </summary>
    public interface IEmailVerificationTokenService
    {
        /// <summary>
        /// Sinh tạo token ngẫu nhiên cho xác minh email.
        /// </summary>
        string GenerateVerificationToken();
    }
}
