using System.Net;
using TutorConnect.API.Models;
using TutorConnect.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace TutorConnect.API.Common
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, messages) = exception switch
            {
                NotFoundException =>
                    (HttpStatusCode.NotFound, new[] { exception.Message }),
                ArgumentException =>
                    (HttpStatusCode.BadRequest, new[] { exception.Message }),
                _ =>
                    (HttpStatusCode.InternalServerError,
                        new[] { "An unexpected error occurred." })
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception while processing {Path}",
                    httpContext.Request.Path);
            }

            httpContext.Response.StatusCode = (int)statusCode;
            await httpContext.Response.WriteAsJsonAsync(
                ApiResponse.Fail(statusCode, messages),
                cancellationToken);

            return true;
        }
    }
}
