using MangaReaderLib.DTOs.Chapters;
using MangaReaderLib.DTOs.Common;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaReaderLib.Services.Interfaces
{
    /// <summary>
    /// Client service để tương tác với Chapter API endpoints
    /// </summary>
    public interface IChapterClient : IChapterReader, IChapterWriter
    {
    }
} 