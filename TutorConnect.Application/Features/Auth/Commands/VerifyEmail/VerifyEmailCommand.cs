using MediatR;
using TutorConnect.Application.Features.Auth.DTOs;

namespace TutorConnect.Application.Features.Auth.Commands.VerifyEmail
{
    public class VerifyEmailCommand : IRequest<VerifyEmailResponse>
    {
        public string Token { get; set; } = null!;

        public VerifyEmailCommand() { }

        public VerifyEmailCommand(VerifyEmailRequest request)
        {
            Token = request.Token;
        }
    }
}
