namespace TutorConnect.Application.Common.Models
{
    public class PagedResponse<T> : ApiResponse<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public static PagedResponse<T> Success(T data, int pageNumber, int pageSize, int totalRecords, string message = "Success", int statusCode = 200)
        {
            var totalPages = totalRecords > 0 ? (int)Math.Ceiling(totalRecords / (double)pageSize) : 0;

            return new PagedResponse<T>
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Message = message,
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                Errors = null
            };
        }
    }
}