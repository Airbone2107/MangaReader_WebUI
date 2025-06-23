using MangaDexLib.Services.APIServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MangaDexLib.Services.APIServices.Services
{
    public class ApiStatusService : BaseApiService, IApiStatusService
    {
        public ApiStatusService(
            HttpClient httpClient,
            ILogger<ApiStatusService> logger,
            IConfiguration configuration,
            IApiRequestHandler apiRequestHandler)
            : base(httpClient, logger, configuration, apiRequestHandler)
        {
        }

        public async Task<bool> TestConnectionAsync()
        {
            var url = BuildUrlWithParams("status");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var result = await GetApiAsync<object>(url, cts.Token);
            return result != null;
        }
    }
} 