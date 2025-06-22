using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces;
using MangaReader.WebUI.Services.UtilityServices;
using MangaReaderLib.DTOs.Chapters;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services
{
    public class MangaDataExtractorService : IMangaDataExtractor
    {
        private readonly LocalizationService _localizationService;

        public MangaDataExtractorService(LocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public string ExtractChapterDisplayTitle(ChapterAttributesDto attributes)
        {
            Debug.Assert(attributes != null, "ChapterAttributes không được null.");

            string chapterNumberString = attributes.ChapterNumber ?? "?";
            string titleFromApi = attributes.Title?.Trim() ?? "";

            if (string.IsNullOrEmpty(attributes.ChapterNumber) || attributes.ChapterNumber == "?")
            {
                return !string.IsNullOrEmpty(titleFromApi) ? titleFromApi : "Oneshot";
            }

            string patternChapterVn = $"^Chương\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";
            string patternChapterEn = $"^Chapter\\s+{Regex.Escape(chapterNumberString)}([:\\s]|$)";

            bool startsWithChapterInfo = Regex.IsMatch(titleFromApi, patternChapterVn, RegexOptions.IgnoreCase) ||
                                         Regex.IsMatch(titleFromApi, patternChapterEn, RegexOptions.IgnoreCase);

            if (startsWithChapterInfo)
            {
                return titleFromApi;
            }
            else if (!string.IsNullOrEmpty(titleFromApi))
            {
                return $"Chương {chapterNumberString}: {titleFromApi}";
            }
            else
            {
                return $"Chương {chapterNumberString}";
            }
        }

        public string ExtractChapterNumber(ChapterAttributesDto attributes)
        {
            Debug.Assert(attributes != null, "ChapterAttributes không được null.");
            return attributes.ChapterNumber ?? "?";
        }

        public string ExtractAndTranslateStatus(string? status)
        {
            return _localizationService.GetStatus(status);
        }
    }
} 