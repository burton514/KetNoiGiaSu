using FluentValidation;

namespace TutorConnect.Application.Features.Auth.Commands.ResendVerificationEmail
{
    public class ResendVerificationEmailCommandValidator : AbstractValidator<ResendVerificationEmailCommand>
    {
        public ResendVerificationEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email là bắt buộc")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.BaseUrl)
                .NotEmpty().WithMessage("BaseUrl là bắt buộc");
        }
    }
}
