namespace MangaReader.WebUI.Services.APIServices
{
    public interface IApiStatusService
    {
        Task<bool> TestConnectionAsync();
    }
} 