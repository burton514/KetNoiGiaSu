using MediatR;
using TutorConnect.Application.Common.Exceptions;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Interfaces;

namespace TutorConnect.Application.Features.Auth.Commands.ResendVerificationEmail
{
    public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand, Unit>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly IEmailVerificationTokenService _emailVerificationTokenService;
        private readonly IEmailService _emailService;

        public ResendVerificationEmailCommandHandler(
            IUserRepository userRepository,
            IEmailVerificationTokenRepository emailVerificationTokenRepository,
            IEmailVerificationTokenService emailVerificationTokenService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _emailVerificationTokenRepository = emailVerificationTokenRepository;
            _emailVerificationTokenService = emailVerificationTokenService;
            _emailService = emailService;
        }

        public async Task<Unit> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
        {
            // Tìm user theo email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException($"Người dùng với email '{request.Email}' không tồn tại");
            }

            // Kiểm tra xem email đã được xác minh chưa
            if (user.Status == TutorConnect.Domain.Enums.UserStatus.Active)
            {
                throw new InvalidOperationException("Email này đã được xác minh rồi");
            }

            // Sinh tạo token xác minh mới
            var token = _emailVerificationTokenService.GenerateVerificationToken();

            // Tạo entity token
            var verificationToken = EmailVerificationToken.Create(user.Id, token);

            // Lưu token vào database
            await _emailVerificationTokenRepository.AddAsync(verificationToken, cancellationToken);
            await _emailVerificationTokenRepository.SaveChangesAsync(cancellationToken);

            // Sinh tạo verification link
            var verificationLink = $"{request.BaseUrl}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";

            // Gửi email
            await _emailService.SendVerificationEmailAsync(
                user.Email,
                user.FullName,
                verificationLink,
                cancellationToken);

            return Unit.Value;
        }
    }
}
