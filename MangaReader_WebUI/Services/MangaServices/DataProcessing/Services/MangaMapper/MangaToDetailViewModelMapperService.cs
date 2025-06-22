using MangaReader.WebUI.Models.ViewModels.Manga;   // ViewModel mới
using MangaReader.WebUI.Models.ViewModels.Chapter; // ViewModel mới
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

/// <summary>
/// Triển khai IMangaToDetailViewModelMapper, chịu trách nhiệm chuyển đổi Manga thành MangaDetailViewModel.
/// </summary>
public class MangaToDetailViewModelMapperService(
    IMangaDataExtractor mangaDataExtractor,
    IMangaToMangaViewModelMapper mangaToMangaViewModelMapper
    ) : IMangaToDetailViewModelMapper
{
    public async Task<MangaDetailViewModel> MapToMangaDetailViewModelAsync(Manga mangaData, List<ChapterViewModel> chapters)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaDetailViewModel.");
        Debug.Assert(mangaData.Attributes != null, "mangaData.Attributes không được null khi mapping thành MangaDetailViewModel.");
        Debug.Assert(chapters != null, "chapters không được null khi mapping thành MangaDetailViewModel.");

        // Map thông tin manga cơ bản
        var mangaViewModel = await mangaToMangaViewModelMapper.MapToMangaViewModelAsync(mangaData);

        // Trích xuất danh sách tiêu đề thay thế đã nhóm
        var alternativeTitlesByLanguage = mangaDataExtractor.ExtractAlternativeTitles(mangaData.Attributes?.AltTitles);

        return new MangaDetailViewModel
        {
            Manga = mangaViewModel,
            Chapters = chapters ?? new List<ChapterViewModel>(), // Đảm bảo không null
            AlternativeTitlesByLanguage = alternativeTitlesByLanguage
        };
    }
} 