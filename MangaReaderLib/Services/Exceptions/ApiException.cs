using MangaReaderLib.DTOs.Common;
using System.Net;

namespace MangaReaderLib.Services.Exceptions
{
    public class ApiException : HttpRequestException
    {
        public ApiErrorResponse? ApiErrorResponse { get; }

        public ApiException(string message, ApiErrorResponse? apiErrorResponse = null, HttpStatusCode? statusCode = null, Exception? inner = null)
            : base(message, inner, statusCode)
        {
            ApiErrorResponse = apiErrorResponse;
        }
    }
} 