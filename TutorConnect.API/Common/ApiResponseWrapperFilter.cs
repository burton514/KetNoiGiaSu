using System.Net;
using TutorConnect.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TutorConnect.API.Common
{
    /// <summary>
    /// Wraps controller results in a consistent ApiResponse envelope.
    /// </summary>
    public sealed class ApiResponseWrapperFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next)
        {
            switch (context.Result)
            {
                case ObjectResult { Value: ApiResponse }:
                    break;

                case ObjectResult objectResult:
                    objectResult.Value = Build(
                        objectResult.Value,
                        objectResult.StatusCode ?? StatusCodes.Status200OK);
                    break;

                case StatusCodeResult statusCodeResult:
                    context.Result = new ObjectResult(
                        Build(null, statusCodeResult.StatusCode))
                    {
                        StatusCode = statusCodeResult.StatusCode
                    };
                    break;

                case EmptyResult:
                    context.Result = new ObjectResult(
                        Build(null, StatusCodes.Status200OK))
                    {
                        StatusCode = StatusCodes.Status200OK
                    };
                    break;
            }

            await next();
        }

        private static ApiResponse Build(object? value, int statusCode)
        {
            return statusCode is >= 200 and < 300
                ? ApiResponse.Success(value, (HttpStatusCode)statusCode)
                : ApiResponse.Fail(
                    (HttpStatusCode)statusCode,
                    ExtractMessages(value, statusCode));
        }

        private static string[] ExtractMessages(object? value, int statusCode)
        {
            return value switch
            {
                ValidationProblemDetails validationProblem =>
                    validationProblem.Errors.SelectMany(entry => entry.Value).ToArray(),
                ProblemDetails problem =>
                    new[] { problem.Detail ?? problem.Title ?? DefaultMessage(statusCode) },
                string message => new[] { message },
                null => new[] { DefaultMessage(statusCode) },
                _ => new[] { value.ToString() ?? DefaultMessage(statusCode) }
            };
        }

        private static string DefaultMessage(int statusCode) =>
            ((HttpStatusCode)statusCode).ToString();
    }
}
