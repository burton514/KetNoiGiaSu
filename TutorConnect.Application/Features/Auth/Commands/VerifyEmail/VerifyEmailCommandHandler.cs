using MediatR;
using TutorConnect.Application.Common.Exceptions;
using TutorConnect.Application.Features.Auth.DTOs;
using TutorConnect.Domain.Interfaces;

namespace TutorConnect.Application.Features.Auth.Commands.VerifyEmail
{
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
    {
        private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;

        public VerifyEmailCommandHandler(
            IEmailVerificationTokenRepository emailVerificationTokenRepository,
            IUserRepository userRepository,
            IEmailService emailService)
        {
            _emailVerificationTokenRepository = emailVerificationTokenRepository;
            _userRepository = userRepository;
            _emailService = emailService;
        }

        public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            // Tìm token xác minh
            var verificationToken = await _emailVerificationTokenRepository.GetByTokenAsync(request.Token, cancellationToken);
            if (verificationToken == null)
            {
                throw new InvalidTokenException("Token xác minh không tồn tại");
            }

            // Kiểm tra token có hợp lệ không
            if (!verificationToken.IsValid)
            {
                throw new InvalidTokenException("Token xác minh đã hết hạn hoặc không hợp lệ");
            }

            // Tìm user
            var user = await _userRepository.GetByIdAsync(verificationToken.UserId, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException($"Người dùng không tồn tại");
            }

            // Đánh dấu token là đã xác minh
            verificationToken.MarkAsVerified();
            await _emailVerificationTokenRepository.UpdateAsync(verificationToken, cancellationToken);

            // Đánh dấu email đã xác minh. KHÔNG gọi user.Activate() - việc xác minh
            // email không được phép tự động mở khóa một tài khoản đang bị Locked
            // (đây chính là lỗ hổng bảo mật đã sửa: Status và xác minh email là
            // hai khái niệm độc lập, xem User.cs).
            user.MarkEmailVerified();
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Gửi email thông báo xác minh thành công
            await _emailService.SendVerificationConfirmationEmailAsync(user.Email, user.FullName, cancellationToken);

            return new VerifyEmailResponse(
                Success: true,
                Message: "Email đã được xác minh thành công. Bạn có thể đăng nhập vào tài khoản của mình.");
        }
    }
}
