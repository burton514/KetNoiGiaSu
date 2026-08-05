using MediatR;
using TutorConnect.Application.Common.Exceptions;
using TutorConnect.Application.Features.Auth.Commands.SendVerificationEmail;
using TutorConnect.Application.Features.Auth.DTOs;
using TutorConnect.Domain.Entities;
using TutorConnect.Domain.Enums;
using TutorConnect.Domain.Interfaces;

namespace TutorConnect.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMediator _mediator;

        public RegisterCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IMediator mediator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _mediator = mediator;
        }

        public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // Kiểm tra email đã tồn tại
            bool emailExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
            if (emailExists)
            {
                throw new UserAlreadyExistsException(request.Email);
            }

            // Validate role
            if (!Enum.TryParse<UserRole>(request.Role, out var role))
            {
                throw new ArgumentException($"Vai trò '{request.Role}' không hợp lệ");
            }

            // Mã hóa mật khẩu
            string passwordHash = _passwordHasher.Hash(request.Password);

            // Tạo user mới với trạng thái Inactive (chờ xác minh email)
            var user = User.Create(
                email: request.Email,
                passwordHash: passwordHash,
                fullName: request.FullName,
                phone: request.Phone,
                role: role,
                timeZoneId: request.TimeZoneId);

            // Đặt status thành Inactive cho đến khi email được xác minh
            user.Deactivate();

            // Lưu vào database
            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Gửi email xác minh
            var baseUrl = string.IsNullOrEmpty(request.BaseUrl) 
                ? "https://localhost:7001"  // Default development URL
                : request.BaseUrl;

            var sendEmailCommand = new SendVerificationEmailCommand(
                userId: user.Id,
                email: user.Email,
                fullName: user.FullName,
                baseUrl: baseUrl);

            await _mediator.Send(sendEmailCommand, cancellationToken);

            return new RegisterResponse(
                UserId: user.Id,
                Email: user.Email,
                FullName: user.FullName,
                Role: user.Role.ToString());
        }
    }
}
