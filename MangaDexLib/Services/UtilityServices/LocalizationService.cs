using MangaDexLib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace MangaDexLib.Services.UtilityServices
{
    public class LocalizationService
    {
        public string GetLocalizedTitle(string titleJson)
        {
            try
            {
                if (string.IsNullOrEmpty(titleJson))
                    return "Không có tiêu đề";

                try
                {
                    var titles = JsonSerializer.Deserialize<Dictionary<string, string>>(titleJson);

                    if (titles == null || titles.Count == 0)
                        return "Không có tiêu đề";

                    if (titles.ContainsKey("vi"))
                        return titles["vi"];
                    if (titles.ContainsKey("en"))
                        return titles["en"];

                    var firstItem = titles.FirstOrDefault();
                    return firstItem.Equals(default(KeyValuePair<string, string>)) ? "Không có tiêu đề" : firstItem.Value;
                }
                catch (JsonException)
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(titleJson);

                        if (jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            if (jsonElement.TryGetProperty("vi", out var viTitle))
                                return viTitle.GetString() ?? "Không có tiêu đề";
                            if (jsonElement.TryGetProperty("en", out var enTitle))
                                return enTitle.GetString() ?? "Không có tiêu đề";

                            using (var properties = jsonElement.EnumerateObject())
                            {
                                if (properties.MoveNext())
                                {
                                    return properties.Current.Value.GetString() ?? "Không có tiêu đề";
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                return "Không có tiêu đề";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi xử lý tiêu đề truyện: {ex.Message}");
                return "Không có tiêu đề";
            }
        }

        public string GetLocalizedDescription(string descriptionJson)
        {
            try
            {
                if (string.IsNullOrEmpty(descriptionJson))
                    return "";

                try
                {
                    var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionJson);

                    if (descriptions == null || descriptions.Count == 0)
                        return "";

                    if (descriptions.ContainsKey("vi"))
                        return descriptions["vi"];
                    if (descriptions.ContainsKey("en"))
                        return descriptions["en"];

                    var firstItem = descriptions.FirstOrDefault();
                    return firstItem.Equals(default(KeyValuePair<string, string>)) ? "" : firstItem.Value;
                }
                catch (JsonException)
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(descriptionJson);

                        if (jsonElement.ValueKind == JsonValueKind.Object)
                        {
                            if (jsonElement.TryGetProperty("vi", out var viDescription))
                                return viDescription.GetString() ?? "";
                            if (jsonElement.TryGetProperty("en", out var enDescription))
                                return enDescription.GetString() ?? "";

                            using (var properties = jsonElement.EnumerateObject())
                            {
                                if (properties.MoveNext())
                                {
                                    return properties.Current.Value.GetString() ?? "";
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi xử lý mô tả truyện: {ex.Message}");
                return "";
            }
        }

        // === ĐÃ SỬA: Chấp nhận tham số có thể null (string?) ===
        public string GetStatus(string? status)
        {
            return status switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }

        public string GetStatus(MangaAttributes? attributes)
        {
            if (attributes == null || string.IsNullOrEmpty(attributes.Status)) return "Không rõ";

            return attributes.Status switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }

        public string GetStatus(Dictionary<string, object> attributesDict)
        {
            string status = attributesDict.ContainsKey("status") ? attributesDict["status"]?.ToString() ?? "unknown" : "unknown";
            return GetStatus(status);
        }
    }
}