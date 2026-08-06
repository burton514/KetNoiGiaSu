using System.Security.Cryptography;
using System.Text;
using TutorConnect.Domain.Interfaces;

namespace TutorConnect.Infrastructure.SqlServer.Services
{
    /// <summary>
    /// Dịch vụ sinh tạo token xác minh email.
    /// </summary>
    public class EmailVerificationTokenService : IEmailVerificationTokenService
    {
        public string GenerateVerificationToken()
        {
            // Sinh tạo token ngẫu nhiên 32 bytes và encode thành Base64
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}
