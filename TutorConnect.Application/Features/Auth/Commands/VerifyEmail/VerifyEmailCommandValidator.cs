using FluentValidation;

namespace TutorConnect.Application.Features.Auth.Commands.VerifyEmail
{
    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token xác minh không được để trống");
        }
    }
}
