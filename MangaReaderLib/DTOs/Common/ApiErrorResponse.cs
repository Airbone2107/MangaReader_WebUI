using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MangaReaderLib.DTOs.Common
{
    public class ApiErrorResponse
    {
        [JsonPropertyName("result")]
        [JsonPropertyOrder(1)]
        public string Result { get; set; } = "error";

        [JsonPropertyName("errors")]
        [JsonPropertyOrder(2)]
        public List<ApiError> Errors { get; set; } = new List<ApiError>();

        public ApiErrorResponse() { }

        public ApiErrorResponse(ApiError error)
        {
            Errors.Add(error);
        }

        public ApiErrorResponse(IEnumerable<ApiError> errors)
        {
            Errors.AddRange(errors);
        }
    }
} 