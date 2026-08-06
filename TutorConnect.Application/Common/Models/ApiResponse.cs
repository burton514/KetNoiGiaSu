namespace TutorConnect.Application.Common.Models
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Success", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Message = message,
                Data = data,
                Errors = null
            };
        }

        public static ApiResponse<T> Failure(List<string> errors, string message = "Error occurred", int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                StatusCode = statusCode,
                Message = message,
                Data = default,
                Errors = errors
            };
        }

        public static ApiResponse<T> Failure(string error, string message = "Error occurred", int statusCode = 400)
        {
            return Failure(new List<string> { error }, message, statusCode);
        }
    }
}