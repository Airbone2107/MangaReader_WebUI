using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        public PagedResult(List<T> items, int total, int offset, int limit)
        {
            Items = items;
            Total = total;
            Offset = offset;
            Limit = limit;
        }
    }
} 