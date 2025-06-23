namespace MangaReader.WebUI.Services.UtilityServices
{
    public class LocalizationService
    {
        private readonly ILogger<LocalizationService> _logger;

        public LocalizationService(ILogger<LocalizationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Lấy trạng thái đã dịch từ chuỗi status.
        /// Phương thức này nhận một chuỗi (ví dụ: "Ongoing", "Completed") và trả về bản dịch tiếng Việt tương ứng.
        /// </summary>
        /// <param name="status">Chuỗi trạng thái, không phân biệt chữ hoa chữ thường.</param>
        /// <returns>Chuỗi tiếng Việt đã được dịch, hoặc "Không rõ" nếu không khớp.</returns>
        public string GetStatus(string? status)
        {
            if (string.IsNullOrEmpty(status))
            {
                _logger.LogWarning("[LOGGING - Localization] GetStatus nhận đầu vào là null hoặc rỗng.");
                return "Không rõ";
            }

            string lowerStatus = status.ToLowerInvariant();
            string result = lowerStatus switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };

            if (result == "Không rõ")
            {
                _logger.LogWarning("[LOGGING - Localization] Không tìm thấy bản dịch cho status '{Status}'. Trả về 'Không rõ'.", status);
            }

            return result;
        }
    }
}