using System.Net;
using System.Text.Json.Serialization;

namespace TutorConnect.API.Models
{
    /// <summary>
    /// Consistent response envelope used by API controllers and exception handling.
    /// </summary>
    public sealed class ApiResponse
    {
        [JsonPropertyName("statusCode")]
        public HttpStatusCode StatusCode { get; set; }

        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; } = true;

        [JsonPropertyName("errorMessages")]
        public List<string> ErrorMessages { get; set; } = new();

        [JsonPropertyName("result")]
        public object? Result { get; set; }

        public static ApiResponse Success(
            object? result,
            HttpStatusCode statusCode = HttpStatusCode.OK) => new()
        {
            StatusCode = statusCode,
            IsSuccess = true,
            Result = result
        };

        public static ApiResponse Fail(
            HttpStatusCode statusCode,
            params string[] errorMessages) => new()
        {
            StatusCode = statusCode,
            IsSuccess = false,
            ErrorMessages = new List<string>(errorMessages),
            Result = null
        };
    }
}
