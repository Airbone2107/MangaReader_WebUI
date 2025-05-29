using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Common
{
    public class ApiResponse
    {
        [JsonPropertyName("result")]
        [JsonPropertyOrder(1)]
        public string Result { get; set; } = "ok";

        public ApiResponse() { }

        public ApiResponse(string result)
        {
            Result = result;
        }
    }

    public class ApiResponse<TData> : ApiResponse
    {
        [JsonPropertyName("response")]
        [JsonPropertyOrder(2)]
        public string ResponseType { get; set; } = "entity";

        [JsonPropertyName("data")]
        [JsonPropertyOrder(3)]
        public TData Data { get; set; } = default!;

        public ApiResponse(TData data, string responseType = "entity") : base()
        {
            Data = data;
            ResponseType = responseType;
        }
    }
} 