using MediatR;
using TutorConnect.Application.Features.Auth.DTOs;

namespace TutorConnect.Application.Features.Auth.Commands.SendVerificationEmail
{
    public class SendVerificationEmailCommand : IRequest<Unit>
    {
        public long UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;

        public SendVerificationEmailCommand() { }

        public SendVerificationEmailCommand(long userId, string email, string fullName, string baseUrl)
        {
            UserId = userId;
            Email = email;
            FullName = fullName;
            BaseUrl = baseUrl;
        }
    }
}
