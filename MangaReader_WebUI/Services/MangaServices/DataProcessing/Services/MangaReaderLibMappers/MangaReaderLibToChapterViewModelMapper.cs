using MangaReader.WebUI.Models;           // Cho ChapterViewModel, ChapterRelationship
using MangaReaderLib.DTOs.Common;        // Cho ResourceObject
using MangaReaderLib.DTOs.Chapters;      // Cho ChapterAttributesDto
using MangaReader.WebUI.Services.MangaServices.DataProcessing.Interfaces.MangaReaderLibMappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Collections.Generic;       // Cho List
using System.Linq; // Thêm using này
using System.Text.RegularExpressions; // Thêm using cho Regex
using System.Text.Json; // Thêm using cho JsonSerializer

namespace MangaReader.WebUI.Services.MangaServices.DataProcessing.Services.MangaReaderLibMappers
{
    public class MangaReaderLibToChapterViewModelMapper : IMangaReaderLibToChapterViewModelMapper
    {
        private readonly ILogger<MangaReaderLibToChapterViewModelMapper> _logger;

        public MangaReaderLibToChapterViewModelMapper(ILogger<MangaReaderLibToChapterViewModelMapper> logger)
        {
            _logger = logger;
        }

        public ChapterViewModel MapToChapterViewModel(ResourceObject<ChapterAttributesDto> chapterData, string translatedLanguage)
        {
            Debug.Assert(chapterData != null, "chapterData không được null khi mapping thành ChapterViewModel.");
            Debug.Assert(chapterData.Attributes != null, "chapterData.Attributes không được null khi mapping thành ChapterViewModel.");

            var attributes = chapterData.Attributes!;
            var relationships = new List<ChapterRelationship>();

            _logger.LogInformation("[MRLib Chapter Mapper] Bắt đầu map chapter ID: {ChapterId}. Dữ liệu Attributes đầu vào: {AttributesJson}",
                chapterData.Id, JsonSerializer.Serialize(attributes));

            try
            {
                string chapterNumForDisplay = attributes.ChapterNumber ?? "?";
                string titleFromApi = attributes.Title?.Trim() ?? "";
                string nameOnlyPart = titleFromApi;

                _logger.LogDebug("[MRLib Chapter Mapper] Chapter ID: {ChapterId} - Input: ChapterNumber='{ChapterNumber}', Title='{TitleFromApi}'",
                    chapterData.Id, attributes.ChapterNumber, titleFromApi);

                // Danh sách các pattern tiền tố cần loại bỏ (sắp xếp từ dài đến ngắn để tránh lỗi greedy matching)
                // Bao gồm cả các biến thể có và không có dấu hai chấm, có và không có số chương
                string[] prefixPatterns = {
                    $"Chương {chapterNumForDisplay}:",
                    $"Chapter {chapterNumForDisplay}:",
                    $"Ch. {chapterNumForDisplay}:",
                    $"Chap {chapterNumForDisplay}:",
                    $"Ch {chapterNumForDisplay}:",
                    // Các dạng không có tên riêng, chỉ có số chương và tiền tố
                    $"Chương {chapterNumForDisplay}",
                    $"Chapter {chapterNumForDisplay}",
                    $"Ch. {chapterNumForDisplay}",
                    $"Chap {chapterNumForDisplay}",
                    $"Ch {chapterNumForDisplay}"
                };
                
                bool prefixRemovedThisIteration;
                do
                {
                    prefixRemovedThisIteration = false;
                    string originalNamePart = nameOnlyPart;

                    foreach (var pattern in prefixPatterns)
                    {
                        // Sử dụng Regex để loại bỏ không phân biệt hoa thường và có thể có khoảng trắng dư thừa
                        var regex = new Regex($"^\\s*{Regex.Escape(pattern)}\\s*", RegexOptions.IgnoreCase);
                        if (regex.IsMatch(nameOnlyPart))
                        {
                            nameOnlyPart = regex.Replace(nameOnlyPart, "", 1).Trim();
                            _logger.LogDebug("[MRLib Chapter Mapper] Chapter ID: {ChapterId} - Loại bỏ prefix '{Pattern}'. nameOnlyPart hiện tại: '{ResultNamePart}'", 
                                chapterData.Id, pattern, nameOnlyPart);
                            prefixRemovedThisIteration = true;
                            break; // Sau khi loại bỏ một prefix, bắt đầu lại vòng lặp ngoài để xử lý lồng nhau
                        }
                    }
                    // Nếu không có prefix nào được loại bỏ nữa, thoát vòng lặp do-while
                    if (!prefixRemovedThisIteration && nameOnlyPart == originalNamePart) break;
                    
                } while (prefixRemovedThisIteration && nameOnlyPart.Length > 0);

                _logger.LogDebug("[MRLib Chapter Mapper] Chapter ID: {ChapterId} - nameOnlyPart sau khi làm sạch: '{NameOnlyPart}'", chapterData.Id, nameOnlyPart);

                string displayTitle;
                // Nếu sau khi làm sạch, nameOnlyPart rỗng hoặc chỉ còn lại là số chương
                if (string.IsNullOrWhiteSpace(nameOnlyPart) || nameOnlyPart.Equals(chapterNumForDisplay, StringComparison.OrdinalIgnoreCase))
                {
                    displayTitle = $"Chương {chapterNumForDisplay}";
                    _logger.LogDebug("[MRLib Chapter Mapper] Chapter ID: {ChapterId} - displayTitle trường hợp 1 (chỉ số chương): '{DisplayTitle}'", chapterData.Id, displayTitle);
                }
                else
                {
                    // nameOnlyPart bây giờ là tên riêng của chương (đã được làm sạch)
                    displayTitle = $"Chương {chapterNumForDisplay}: {nameOnlyPart}";
                     _logger.LogDebug("[MRLib Chapter Mapper] Chapter ID: {ChapterId} - displayTitle trường hợp 2 (số chương + tên riêng): '{DisplayTitle}'", chapterData.Id, displayTitle);
                }

                // _logger.LogDebug("Final display title for ChapterId {ChapterId}: '{DisplayTitle}' (RawNumber: '{RawNum}', RawApiTitle: '{RawApiTitle}', CleanedNamePart: '{CleanedNamePart}')", 
                //     chapterData.Id, displayTitle, attributes.ChapterNumber, attributes.Title, nameOnlyPart);

                if (chapterData.Relationships != null && chapterData.Relationships.Any())
                {
                    relationships.AddRange(chapterData.Relationships
                        .Where(r => r != null && !string.IsNullOrEmpty(r.Id) && !string.IsNullOrEmpty(r.Type))
                        .Select(r => new ChapterRelationship { Id = r.Id, Type = r.Type })
                    );
                }

                var resultViewModel = new ChapterViewModel
                {
                    Id = chapterData.Id,
                    Title = displayTitle,
                    Number = chapterNumForDisplay, 
                    Volume = attributes.Volume ?? "Không rõ",
                    Language = translatedLanguage,
                    PublishedAt = attributes.PublishAt,
                    Relationships = relationships
                };
                 _logger.LogInformation("[MRLib Chapter Mapper] Kết thúc map chapter ID: {ChapterId}. ViewModel kết quả: {ViewModelJson}",
                    chapterData.Id, JsonSerializer.Serialize(resultViewModel));
                return resultViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MRLib Chapter Mapper] Lỗi khi mapping MangaReaderLib ChapterData thành ChapterViewModel cho ID: {ChapterId}", chapterData?.Id);
                return new ChapterViewModel
                {
                    Id = chapterData?.Id ?? "error",
                    Title = "Lỗi tải chương",
                    Number = "?",
                    Volume = "Lỗi",
                    Language = translatedLanguage,
                    PublishedAt = DateTime.MinValue,
                    Relationships = new List<ChapterRelationship>()
                };
            }
        }
    }
} 