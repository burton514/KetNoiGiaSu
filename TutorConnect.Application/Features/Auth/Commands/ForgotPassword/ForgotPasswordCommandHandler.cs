using MediatR;
using TutorConnect.Application.Common.Exceptions;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Interfaces;

namespace TutorConnect.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IEmailVerificationTokenService _tokenService;
        private readonly IEmailService _emailService;

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IEmailVerificationTokenService tokenService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            // Tìm user theo email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                // Không tiết lộ thông tin user tồn tại hay không (bảo mật)
                return Unit.Value;
            }

            // Kiểm tra email đã được xác minh
            if (!user.CanSignIn)
            {
                throw new UnauthorizedException("Email chưa được xác minh. Vui lòng xác minh email trước khi đặt lại mật khẩu");
            }

            // Sinh tạo token đặt lại mật khẩu
            var token = _tokenService.GenerateVerificationToken();

            // Tạo entity token
            var resetToken = PasswordResetToken.Create(user.Id, token);

            // Lưu token vào database
            await _passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);
            await _passwordResetTokenRepository.SaveChangesAsync(cancellationToken);

            // Sinh tạo reset link
            var resetLink = $"{request.BaseUrl}/api/auth/reset-password?token={Uri.EscapeDataString(token)}";

            // Gửi email với link đặt lại mật khẩu
            await _emailService.SendPasswordResetEmailAsync(
                user.Email,
                user.FullName,
                resetLink,
                cancellationToken);

            return Unit.Value;
        }
    }
}
