using FluentValidation;

namespace TutorConnect.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email là bắt buộc")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu là bắt buộc")
                .MinimumLength(8).WithMessage("Mật khẩu phải ít nhất 8 ký tự");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Tên đầy đủ là bắt buộc")
                .Length(2, 100).WithMessage("Tên đầy đủ phải từ 2 đến 100 ký tự");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Vai trò là bắt buộc")
                .Must(r => r == "Admin" || r == "Tutor" || r == "Student")
                .WithMessage("Vai trò phải là Admin, Tutor hoặc Student");

            RuleFor(x => x.TimeZoneId)
                .NotEmpty().WithMessage("Múi giờ là bắt buộc");
        }
    }
}
