using MediatR;

namespace TutorConnect.Application.Features.Auth.Commands.ResendVerificationEmail
{
    public class ResendVerificationEmailCommand : IRequest<Unit>
    {
        public string Email { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;

        public ResendVerificationEmailCommand() { }

        public ResendVerificationEmailCommand(string email, string baseUrl)
        {
            Email = email;
            BaseUrl = baseUrl;
        }
    }
}
