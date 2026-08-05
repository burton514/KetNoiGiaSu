using System.Text.Json.Serialization;

namespace TutorConnect.Application.Common.Models
{
    public sealed class PaginationResponse<T>
    {
        [JsonPropertyName("items")]
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

        [JsonPropertyName("hasPrevious")]
        public bool HasPrevious => PageNumber > 1;

        [JsonPropertyName("hasNext")]
        public bool HasNext => PageNumber < TotalPages;

        public PaginationResponse()
        {
        }

        public PaginationResponse(
            IReadOnlyList<T> items,
            int totalCount,
            int pageNumber,
            int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
