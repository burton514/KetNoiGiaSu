using MediatR;
using TutorConnect.Application.Features.Auth.DTOs;

namespace TutorConnect.Application.Features.Auth.Commands.Register
{
    public class RegisterCommand : IRequest<RegisterResponse>
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string Role { get; set; } = null!;
        public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
        public string? BaseUrl { get; set; }

        public RegisterCommand() { }

        public RegisterCommand(RegisterRequest request)
        {
            Email = request.Email;
            Password = request.Password;
            FullName = request.FullName;
            Phone = request.Phone;
            Role = request.Role;
            TimeZoneId = request.TimeZoneId;
        }
    }
}
