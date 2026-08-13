using MediatR;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Interfaces;

namespace TutorConnect.Application.Features.Auth.Commands.SendVerificationEmail
{
    public class SendVerificationEmailCommandHandler : IRequestHandler<SendVerificationEmailCommand, Unit>
    {
        private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly IEmailVerificationTokenService _emailVerificationTokenService;
        private readonly IEmailService _emailService;

        public SendVerificationEmailCommandHandler(
            IEmailVerificationTokenRepository emailVerificationTokenRepository,
            IEmailVerificationTokenService emailVerificationTokenService,
            IEmailService emailService)
        {
            _emailVerificationTokenRepository = emailVerificationTokenRepository;
            _emailVerificationTokenService = emailVerificationTokenService;
            _emailService = emailService;
        }

        public async Task<Unit> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
        {
            // Sinh tạo token xác minh
            var token = _emailVerificationTokenService.GenerateVerificationToken();

            // Tạo entity token
            var verificationToken = EmailVerificationToken.Create(request.UserId, token);

            // Lưu token vào database
            await _emailVerificationTokenRepository.AddAsync(verificationToken, cancellationToken);
            await _emailVerificationTokenRepository.SaveChangesAsync(cancellationToken);

            // Sinh tạo verification link
            var verificationLink = $"{request.BaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";

            // Gửi email
            await _emailService.SendVerificationEmailAsync(
                request.Email,
                request.FullName,
                verificationLink,
                cancellationToken);

            return Unit.Value;
        }
    }
}
