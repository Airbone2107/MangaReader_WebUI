namespace MangaReader.WebUI.Services.UtilityServices
{
    public class LocalizationService
    {
        /// <summary>
        /// Lấy trạng thái đã dịch từ chuỗi status.
        /// Phương thức này nhận một chuỗi (ví dụ: "Ongoing", "Completed") và trả về bản dịch tiếng Việt tương ứng.
        /// </summary>
        /// <param name="status">Chuỗi trạng thái, không phân biệt chữ hoa chữ thường.</param>
        /// <returns>Chuỗi tiếng Việt đã được dịch, hoặc "Không rõ" nếu không khớp.</returns>
        public string GetStatus(string? status)
        {
            if (string.IsNullOrEmpty(status)) return "Không rõ";

            return status.ToLowerInvariant() switch
            {
                "ongoing" => "Đang tiến hành",
                "completed" => "Hoàn thành",
                "hiatus" => "Tạm ngưng",
                "cancelled" => "Đã hủy",
                _ => "Không rõ"
            };
        }
    }
}