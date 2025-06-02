using MangaReaderLib.Services.Interfaces; // Cho IApiClient từ MangaReaderLib

namespace MangaReader.WebUI.Services.APIServices.MangaReaderLibApiClients.Interfaces
{
    // Wrapper cho IApiClient của MangaReaderLib để có thể inject vào WebUI
    public interface IMangaReaderLibApiClient : IApiClient
    {
    }
} 