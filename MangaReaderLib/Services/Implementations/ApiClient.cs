using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MangaReaderLib.Services.Exceptions;

namespace MangaReaderLib.Services.Implementations
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogInformation("Executing GET request to {RequestUri}", requestUri);
                var response = await _httpClient.GetAsync(requestUri, cancellationToken);
                
                return await HandleResponseAsync<T>(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during GET request to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class 
            where TResponse : class
        {
            try
            {
                _logger.LogInformation("Executing POST request to {RequestUri}", requestUri);
                var jsonContent = CreateJsonContent(content);
                var response = await _httpClient.PostAsync(requestUri, jsonContent, cancellationToken);
                
                return await HandleResponseAsync<TResponse>(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during POST request to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task PostAsync<TRequest>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class
        {
            try
            {
                _logger.LogInformation("Executing POST request to {RequestUri}", requestUri);
                var jsonContent = CreateJsonContent(content);
                var response = await _httpClient.PostAsync(requestUri, jsonContent, cancellationToken);
                
                await EnsureSuccessStatusCodeAsync(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during POST request to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task<TResponse?> PostAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default) 
            where TResponse : class
        {
            try
            {
                _logger.LogInformation("Executing POST request with HttpContent to {RequestUri}", requestUri);
                var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
                
                return await HandleResponseAsync<TResponse>(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during POST request with HttpContent to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class 
            where TResponse : class
        {
            try
            {
                _logger.LogInformation("Executing PUT request to {RequestUri}", requestUri);
                var jsonContent = CreateJsonContent(content);
                var response = await _httpClient.PutAsync(requestUri, jsonContent, cancellationToken);
                
                return await HandleResponseAsync<TResponse>(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during PUT request to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task PutAsync<TRequest>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class
        {
            try
            {
                _logger.LogInformation("Executing PUT request to {RequestUri}", requestUri);
                var jsonContent = CreateJsonContent(content);
                var response = await _httpClient.PutAsync(requestUri, jsonContent, cancellationToken);
                
                await EnsureSuccessStatusCodeAsync(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during PUT request to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task<TResponse?> PutAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            where TResponse : class
        {
            try
            {
                _logger.LogInformation("Executing PUT request with HttpContent to {RequestUri}", requestUri);
                var response = await _httpClient.PutAsync(requestUri, content, cancellationToken);
                return await HandleResponseAsync<TResponse>(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during PUT request with HttpContent to {RequestUri}", requestUri);
                throw;
            }
        }

        public async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Executing DELETE request to {RequestUri}", requestUri);
                var response = await _httpClient.DeleteAsync(requestUri, cancellationToken);
                
                await EnsureSuccessStatusCodeAsync(response, cancellationToken);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Error during DELETE request to {RequestUri}", requestUri);
                throw;
            }
        }

        private StringContent CreateJsonContent<T>(T content) where T : class
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            return stringContent;
        }

        private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                try
                {
                    return await JsonSerializer.DeserializeAsync<T>(content, _jsonOptions, cancellationToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing response from {RequestUri}", response.RequestMessage?.RequestUri);
                    throw new InvalidOperationException("Invalid response format received from server.", ex);
                }
            }
            else
            {
                await HandleErrorResponseAsync(response, cancellationToken);
                // Dòng code dưới đây không bao giờ được thực thi vì HandleErrorResponseAsync sẽ ném exception
                return null;
            }
        }

        private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, cancellationToken);
            }
        }

        private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            ApiErrorResponse? errorResponse = null;
            try
            {
                // Thử parse lỗi dưới dạng ApiErrorResponse
                errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse error response as ApiErrorResponse. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, content);
                // Fall through to generic HttpRequestException or ApiException if parsing fails
            }

            if (errorResponse?.Errors?.Count > 0)
            {
                // Thay vì ném HttpRequestException với chuỗi, ném ApiException chứa toàn bộ đối tượng lỗi
                throw new ApiException(
                    errorResponse.Errors[0].Detail ?? errorResponse.Errors[0].Title, // Message cho exception
                    errorResponse,                                                   // Đối tượng ApiErrorResponse đầy đủ
                    response.StatusCode                                              // Status Code
                );
            }
            else
            {
                // Fallback cho các trường hợp không có JSON lỗi có cấu trúc hoặc không có lỗi cụ thể
                throw new HttpRequestException($"API request failed with status code: {(int)response.StatusCode} - {response.ReasonPhrase}", 
                    null, response.StatusCode);
            }
        }
    }
} 