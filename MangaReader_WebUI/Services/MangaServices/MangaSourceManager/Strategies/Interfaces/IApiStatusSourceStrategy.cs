// File: MangaReader_WebUI\Services\MangaServices\MangaSourceManager\Strategies\Interfaces\IApiStatusSourceStrategy.cs
namespace MangaReader.WebUI.Services.MangaServices.MangaSourceManager.Strategies.Interfaces
{
    public interface IApiStatusSourceStrategy
    {
        Task<bool> TestConnectionAsync();
    }
} 