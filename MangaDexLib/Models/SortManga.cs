namespace MangaDexLib.Models
{
    public class SortManga
    {
        public string? Title { get; set; }
        public List<string>? ContentRating { get; set; } = new List<string>();
        public string? Status { get; set; }
        public string? OrderBy { get; set; }
        public string? OrderDirection { get; set; }

        public Dictionary<string, object> ToParams()
        {
            var result = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(Title))
            {
                result["title"] = Title;
            }

            if (!string.IsNullOrEmpty(Status))
            {
                result["status[]"] = Status;
            }

            if (!string.IsNullOrEmpty(OrderBy))
            {
                string direction = OrderDirection ?? "desc";
                result[$"order[{OrderBy}]"] = direction;
            }

            return result;
        }
    }
} 