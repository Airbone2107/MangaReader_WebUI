using MangaReaderLib.Services.Implementations;
using MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces;
using Microsoft.Extensions.Logging;
using MangaReaderLib.DTOs.Common;
using MangaReaderLib.Services.Interfaces;
using System.Net.Http; // Cần thiết để sử dụng HttpContent

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Services
{
    // Đây là một lớp wrapper cho IApiClient từ MangaReaderLib
    // Mục đích:
    // 1. Đảm bảo IApiClient của MangaReaderLib được inject với HttpClient đúng (MangaReaderLibApiClient)
    // 2. Cung cấp một interface riêng biệt trong WebUI cho client này để dễ quản lý DI
    public class MangaReaderLibApiClientService : IMangaReaderLibApiClient
    {
        private readonly IApiClient _innerClient;
        private readonly ILogger<MangaReaderLibApiClientService> _wrapperLogger;

        public MangaReaderLibApiClientService(
            HttpClient httpClient, 
            ILogger<ApiClient> innerClientLogger, 
            ILogger<MangaReaderLibApiClientService> wrapperLogger)
        {
            _wrapperLogger = wrapperLogger;
            // Khởi tạo client từ thư viện MangaReaderLib
            // HttpClient được inject vào đây phải là instance "MangaReaderLibApiClient"
            _innerClient = new ApiClient(httpClient, innerClientLogger);
        }

        public async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default) where T : class
        {
            _wrapperLogger.LogInformation("MangaReaderLibApiClientService (Wrapper): GET {RequestUri}", requestUri);
            return await _innerClient.GetAsync<T>(requestUri, cancellationToken);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            _wrapperLogger.LogInformation("MangaReaderLibApiClientService (Wrapper): POST {RequestUri}", requestUri);
            return await _innerClient.PostAsync<TRequest, TResponse>(requestUri, content, cancellationToken);
        }

        public async Task PostAsync<TRequest>(string requestUri, TRequest content, CancellationToken cancellationToken = default) where TRequest : class
        {
            _wrapperLogger.LogInformation("MangaReaderLibApiClientService (Wrapper): POST (void) {RequestUri}", requestUri);
            await _innerClient.PostAsync(requestUri, content, cancellationToken);
        }

        public async Task<TResponse?> PostAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default) where TResponse : class
        {
            _wrapperLogger.LogInformation("MangaReaderLibApiClientService (Wrapper): POST (HttpContent) {RequestUri}", requestUri);
            return await _innerClient.PostAsync<TResponse>(requestUri, content, cancellationToken);
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            _wrapperLogger.LogInformation("MangaReaderLibApiClientService (Wrapper): PUT {RequestUri}", requestUri);
            return await _innerClient.PutAsync<TRequest, TResponse>(requestUri, content, cancellationToken);
        }

        public async Task PutAsync<TRequest>(string requestUri, TRequest content, CancellationToken cancellationToken = default) where TRequest : class
        {
            _wrapperLogger.LogInformation("MangaReaderLibApiClientService (Wrapper): PUT (void) {RequestUri}", requestUri);
            await _innerClient.PutAsync(requestUri, content, cancellationToken);
        }

        public async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            _wrapperLogger.LogInformation("MangaReaderLibApiClientService (Wrapper): DELETE {RequestUri}", requestUri);
            await _innerClient.DeleteAsync(requestUri, cancellationToken);
        }
    }
} 