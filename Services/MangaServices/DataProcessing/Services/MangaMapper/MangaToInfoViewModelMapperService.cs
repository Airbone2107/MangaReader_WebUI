using MangaReader.WebUI.Models;
using MangaReader.WebUI.Models.Mangadex;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaMapper;
using MangaReader.WebUI.Services.MangaServices.Models;
using System.Diagnostics;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaMapper;

/// <summary>
/// Triển khai IMangaToInfoViewModelMapper, chịu trách nhiệm chuyển đổi Manga thành MangaInfoViewModel.
/// </summary>
public class MangaToInfoViewModelMapperService(
    IMangaDataExtractor mangaDataExtractor
    ) : IMangaToInfoViewModelMapper
{
    public MangaInfoViewModel MapToMangaInfoViewModel(Manga mangaData)
    {
        Debug.Assert(mangaData != null, "mangaData không được null khi mapping thành MangaInfoViewModel.");

        string id = mangaData.Id.ToString();
        var attributes = mangaData.Attributes;
        var relationships = mangaData.Relationships;

        string title = "Lỗi tải tiêu đề";
        if (attributes != null)
        {
            title = mangaDataExtractor.ExtractMangaTitle(attributes.Title, attributes.AltTitles);
        }

        string coverUrl = mangaDataExtractor.ExtractCoverUrl(id, relationships);

        // MangaInfoViewModel không cần trạng thái follow hay các thông tin phức tạp khác
        return new MangaInfoViewModel
        {
            MangaId = id,
            MangaTitle = title,
            CoverUrl = coverUrl
        };
    }
} 