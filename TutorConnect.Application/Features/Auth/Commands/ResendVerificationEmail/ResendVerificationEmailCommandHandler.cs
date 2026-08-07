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

            // Tài khoản đang bị khóa KHÔNG được phép gửi lại email xác minh - xác
            // minh email không được dùng để mở khóa tài khoản (tách biệt Status
            // và IsEmailVerified, xem User.cs).
            if (user.Status == TutorConnect.Domain.Enums.UserStatus.Locked)
            {
                throw new UnauthorizedException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên");
            }

            // Kiểm tra xem email đã được xác minh chưa (dựa trên IsEmailVerified,
            // không dựa trên Status)
            if (user.IsEmailVerified)
            {
                throw new InvalidOperationException("Email này đã được xác minh rồi");
            }

            // Sinh tạo token xác minh mới (raw token gửi qua email, chỉ hash được lưu DB)
            var rawToken = _emailVerificationTokenService.GenerateVerificationToken();

            // Tạo entity token
            var verificationToken = EmailVerificationToken.Create(user.Id, rawToken);

            // Lưu token vào database
            await _emailVerificationTokenRepository.AddAsync(verificationToken, cancellationToken);
            await _emailVerificationTokenRepository.SaveChangesAsync(cancellationToken);

            // Link trỏ về trang Frontend (SPA) - trang này sẽ đọc token từ URL rồi
            // tự gọi API backend. request.BaseUrl giờ lấy từ cấu hình Frontend:BaseUrl,
            // KHÔNG từ Request.Host, để tránh Host Header Injection (xem AuthController).
            var verificationLink = $"{request.BaseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";

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
