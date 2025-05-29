using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client cơ bản để tương tác với API, cung cấp các phương thức chung cho HTTP requests
    /// </summary>
    public interface IApiClient
    {
        /// <summary>
        /// Thực hiện HTTP GET request và deserialize kết quả thành đối tượng kiểu T
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng trả về</typeparam>
        /// <param name="requestUri">URI endpoint, không bao gồm base URL</param>
        /// <param name="cancellationToken">Token để hủy thao tác</param>
        /// <returns>Đối tượng đã được deserialize từ JSON response</returns>
        Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Thực hiện HTTP POST request với nội dung là đối tượng được serialize thành JSON
        /// </summary>
        /// <typeparam name="TRequest">Kiểu dữ liệu của đối tượng gửi đi</typeparam>
        /// <typeparam name="TResponse">Kiểu dữ liệu của đối tượng trả về</typeparam>
        /// <param name="requestUri">URI endpoint, không bao gồm base URL</param>
        /// <param name="content">Đối tượng sẽ được serialize thành JSON và gửi đi</param>
        /// <param name="cancellationToken">Token để hủy thao tác</param>
        /// <returns>Đối tượng đã được deserialize từ JSON response</returns>
        Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class 
            where TResponse : class;

        /// <summary>
        /// Thực hiện HTTP POST request với nội dung là đối tượng được serialize thành JSON
        /// </summary>
        /// <typeparam name="TRequest">Kiểu dữ liệu của đối tượng gửi đi</typeparam>
        /// <param name="requestUri">URI endpoint, không bao gồm base URL</param>
        /// <param name="content">Đối tượng sẽ được serialize thành JSON và gửi đi</param>
        /// <param name="cancellationToken">Token để hủy thao tác</param>
        Task PostAsync<TRequest>(string requestUri, TRequest content, CancellationToken cancellationToken = default) where TRequest : class;

        /// <summary>
        /// Thực hiện HTTP POST request với nội dung là HttpContent (MultipartFormDataContent, StringContent, etc.)
        /// </summary>
        /// <typeparam name="TResponse">Kiểu dữ liệu của đối tượng trả về</typeparam>
        /// <param name="requestUri">URI endpoint, không bao gồm base URL</param>
        /// <param name="content">HttpContent để gửi đi (ví dụ: MultipartFormDataContent cho upload file)</param>
        /// <param name="cancellationToken">Token để hủy thao tác</param>
        /// <returns>Đối tượng đã được deserialize từ JSON response</returns>
        Task<TResponse?> PostAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default) where TResponse : class;

        /// <summary>
        /// Thực hiện HTTP PUT request với nội dung là đối tượng được serialize thành JSON
        /// </summary>
        /// <typeparam name="TRequest">Kiểu dữ liệu của đối tượng gửi đi</typeparam>
        /// <typeparam name="TResponse">Kiểu dữ liệu của đối tượng trả về</typeparam>
        /// <param name="requestUri">URI endpoint, không bao gồm base URL</param>
        /// <param name="content">Đối tượng sẽ được serialize thành JSON và gửi đi</param>
        /// <param name="cancellationToken">Token để hủy thao tác</param>
        /// <returns>Đối tượng đã được deserialize từ JSON response</returns>
        Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default) 
            where TRequest : class 
            where TResponse : class;

        /// <summary>
        /// Thực hiện HTTP PUT request với nội dung là đối tượng được serialize thành JSON
        /// </summary>
        /// <typeparam name="TRequest">Kiểu dữ liệu của đối tượng gửi đi</typeparam>
        /// <param name="requestUri">URI endpoint, không bao gồm base URL</param>
        /// <param name="content">Đối tượng sẽ được serialize thành JSON và gửi đi</param>
        /// <param name="cancellationToken">Token để hủy thao tác</param>
        Task PutAsync<TRequest>(string requestUri, TRequest content, CancellationToken cancellationToken = default) where TRequest : class;

        /// <summary>
        /// Thực hiện HTTP DELETE request
        /// </summary>
        /// <param name="requestUri">URI endpoint, không bao gồm base URL</param>
        /// <param name="cancellationToken">Token để hủy thao tác</param>
        Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
    }
} 