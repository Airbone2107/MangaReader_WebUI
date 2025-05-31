using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Common
{
    public class ApiCollectionResponse<TData> : ApiResponse
    {
        [JsonPropertyName("response")]
        [JsonPropertyOrder(2)]
        public string ResponseType { get; set; } = "collection";

        [JsonPropertyName("data")]
        [JsonPropertyOrder(3)]
        public List<TData> Data { get; set; } = new List<TData>();

        [JsonPropertyName("limit")]
        [JsonPropertyOrder(4)]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        [JsonPropertyOrder(5)]
        public int Offset { get; set; }

        [JsonPropertyName("total")]
        [JsonPropertyOrder(6)]
        public int Total { get; set; }

        // Thêm constructor không tham số này
        public ApiCollectionResponse() : base()
        {
            // Constructor không tham số, cần thiết cho deserialization
        }

        public ApiCollectionResponse(List<TData> data, int total, int offset, int limit)
            : base("ok")
        {
            Data = data;
            Total = total;
            Offset = offset;
            Limit = limit;
        }
        
        public ApiCollectionResponse(PagedResult<TData> pagedResult)
            : this(pagedResult.Items, pagedResult.Total, pagedResult.Offset, pagedResult.Limit)
        {
        }
    }
}