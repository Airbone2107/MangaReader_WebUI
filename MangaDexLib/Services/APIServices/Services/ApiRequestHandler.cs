using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace MangaDexLib.Services.APIServices.Services
{
    /// <summary>
    /// Cung cấp việc triển khai cho <see cref="IApiRequestHandler"/>.
    /// Đóng gói logic xử lý yêu cầu HTTP GET, bao gồm gửi yêu cầu,
    /// xử lý phản hồi thành công và lỗi, deserialize JSON, ghi log và đo thời gian thực thi.
    /// </summary>
    public class ApiRequestHandler : IApiRequestHandler
    {
        private readonly ILogger<ApiRequestHandler> _handlerLogger;

        public ApiRequestHandler(ILogger<ApiRequestHandler> handlerLogger)
        {
            _handlerLogger = handlerLogger;
        }

        public async Task<T?> GetAsync<T>(
            HttpClient httpClient,
            string url,
            ILogger logger, // Logger của service gọi đến
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default
        ) where T : class
        {
            logger.LogInformation("Attempting GET request to: {Url}", url);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response == null)
                {
                    stopwatch.Stop();
                    logger.LogError("GET {Url} - Failed: HttpClient returned null response. Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                    return null;
                }

                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                string rawContentForErrorLog = "[Could not read stream]";

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = await JsonSerializer.DeserializeAsync<T>(contentStream, options, cancellationToken);
                        stopwatch.Stop();

                        if (result == null)
                        {
                            logger.LogWarning("GET {Url} - Success (Status: {StatusCode}) but deserialized result was null. Duration: {ElapsedMs}ms", url, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            logger.LogInformation("GET {Url} - Success. Status: {StatusCode}. Duration: {ElapsedMs}ms", url, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
                        }
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        stopwatch.Stop();
                        rawContentForErrorLog = await ReadStreamSafelyAsync(contentStream, _handlerLogger);

                        logger.LogError(jsonEx, "GET {Url} - JSON Deserialization error. Status: {StatusCode}. Duration: {ElapsedMs}ms. Raw Content: {RawContent}", url, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, rawContentForErrorLog);
                        return null;
                    }
                }
                else
                {
                    stopwatch.Stop();
                    rawContentForErrorLog = await ReadStreamSafelyAsync(contentStream, _handlerLogger);

                    LogApiErrorHelper(logger, "GET", url, response, rawContentForErrorLog, stopwatch.ElapsedMilliseconds);
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                stopwatch.Stop();
                logger.LogError(httpEx, "GET {Url} - HTTP Request error (Network/DNS issue?). Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return null;
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                logger.LogWarning("GET {Url} - Request was canceled by cancellation token. Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return null;
            }
            catch (TaskCanceledException timeoutEx)
            {
                stopwatch.Stop();
                logger.LogError(timeoutEx, "GET {Url} - Request timed out. Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "GET {Url} - Unexpected exception during API request. Duration: {ElapsedMs}ms", url, stopwatch.ElapsedMilliseconds);
                return null;
            }
        }

        private async Task<string> ReadStreamSafelyAsync(Stream stream, ILogger localLogger)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            else if (!stream.CanRead)
            {
                return "[Stream not readable or seekable]";
            }

            try
            {
                using var reader = new StreamReader(stream, leaveOpen: true);
                char[] buffer = new char[1024];
                int charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                string content = new string(buffer, 0, charsRead);
                if (stream.Length > 1024 || (!stream.CanSeek && reader.Peek() != -1))
                {
                    content += "... (content truncated)";
                }
                return content;
            }
            catch (Exception ex)
            {
                localLogger.LogError(ex, "Error reading stream content for logging purposes.");
                return "[Error reading stream content]";
            }
        }

        private void LogApiErrorHelper(ILogger logger, string method, string url, HttpResponseMessage response, string content, long durationMs)
        {
            LogLevel logLevel = response.StatusCode >= System.Net.HttpStatusCode.InternalServerError ? LogLevel.Error : LogLevel.Warning;

            logger.Log(logLevel,
                       "{Method} {Url} - API Error. Status: {StatusCode} ({ReasonPhrase}). Duration: {ElapsedMs}ms. Response Content: {ResponseContent}",
                       method.ToUpperInvariant(),
                       url,
                       (int)response.StatusCode,
                       response.ReasonPhrase,
                       durationMs,
                       content);
        }
    }
} 