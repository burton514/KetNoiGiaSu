using System.Security.Cryptography;
using TutorConnect.Domain.Interfaces;

namespace TutorConnect.Infrastructure.SqlServer.Services
{
    /// <summary>
    /// Dịch vụ sinh tạo token xác minh email / đặt lại mật khẩu.
    /// </summary>
    public class EmailVerificationTokenService : IEmailVerificationTokenService
    {
        public string GenerateVerificationToken()
        {
            // Sinh tạo token ngẫu nhiên 32 bytes, encode dạng Base64Url (RFC 4648 §5):
            // không chứa '+', '/', '=' 
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
            }
        }
    }
}
