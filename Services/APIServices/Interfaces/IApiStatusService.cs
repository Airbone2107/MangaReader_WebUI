namespace MangaReader.WebUI.Services.APIServices.Interfaces
{
    public interface IApiStatusService
    {
        Task<bool> TestConnectionAsync();
    }
} 