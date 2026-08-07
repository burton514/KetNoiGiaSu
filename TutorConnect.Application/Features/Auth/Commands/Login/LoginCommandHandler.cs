using MediatR;
using TutorConnect.Application.Common.Exceptions;
using TutorConnect.Application.Features.Auth.DTOs;
using TutorConnect.Domain.Interfaces;

namespace TutorConnect.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Tìm user theo email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                throw new InvalidCredentialsException();
            }

            // Kiểm tra tài khoản có bị khóa không (độc lập với việc email đã xác minh chưa)
            if (!user.CanSignIn)
            {
                throw new UnauthorizedException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên");
            }

            // Kiểm tra email đã được xác minh
            if (!user.IsEmailVerified)
            {
                throw new UnauthorizedException("Email chưa được xác minh. Vui lòng kiểm tra email của bạn để xác minh");
            }

            // Xác minh mật khẩu
            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new InvalidCredentialsException();
            }

            // Sinh tạo tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenHash = _jwtTokenService.GetTokenHash(refreshToken);

            // Lưu refresh token
            var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
                userId: user.Id,
                tokenHash: refreshTokenHash,
                expiresAtUtc: DateTime.UtcNow.AddDays(7));

            await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            return new LoginResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresIn: DateTime.UtcNow.AddHours(1),
                Email: user.Email,
                FullName: user.FullName,
                Role: user.Role.ToString());
        }
    }
}
