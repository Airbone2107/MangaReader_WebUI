using MangaReaderLib.DTOs.Common;
using MangaReaderLib.DTOs.CoverArts;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với CoverArt API endpoints
    /// </summary>
    public interface ICoverArtClient : ICoverArtReader, ICoverArtWriter
    {
    }
} 