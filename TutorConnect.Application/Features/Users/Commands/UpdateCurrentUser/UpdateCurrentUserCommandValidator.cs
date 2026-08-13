using FluentValidation;

namespace TutorConnect.Application.Features.Users.Commands.UpdateCurrentUser
{
    public class UpdateCurrentUserCommandValidator : AbstractValidator<UpdateCurrentUserCommand>
    {
        public UpdateCurrentUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.Phone)
                .MaximumLength(30);

            RuleFor(x => x.TimeZoneId)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
