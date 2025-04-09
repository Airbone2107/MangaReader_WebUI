using System.Text.Json;

namespace manga_reader_web.Services.UtilityServices
{
    public class JsonConversionService
    {
        private readonly ILogger<JsonConversionService> _logger;

        public JsonConversionService(ILogger<JsonConversionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Chuyển đổi JsonElement thành Dictionary
        /// </summary>
        /// <param name="element">JsonElement cần chuyển đổi</param>
        /// <returns>Dictionary chứa thông tin từ JsonElement</returns>
        public Dictionary<string, object> ConvertJsonElementToDict(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            if (element.ValueKind != JsonValueKind.Object)
            {
                return dict;
            }

            foreach (var property in element.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.Object:
                        dict[property.Name] = ConvertJsonElementToDict(property.Value);
                        break;
                    case JsonValueKind.Array:
                        dict[property.Name] = ConvertJsonElementToList(property.Value);
                        break;
                    case JsonValueKind.String:
                        dict[property.Name] = property.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (property.Value.TryGetInt32(out int intValue))
                        {
                            dict[property.Name] = intValue;
                        }
                        else if (property.Value.TryGetInt64(out long longValue))
                        {
                            dict[property.Name] = longValue;
                        }
                        else
                        {
                            dict[property.Name] = property.Value.GetDouble();
                        }
                        break;
                    case JsonValueKind.True:
                        dict[property.Name] = true;
                        break;
                    case JsonValueKind.False:
                        dict[property.Name] = false;
                        break;
                    case JsonValueKind.Null:
                        dict[property.Name] = null;
                        break;
                    default:
                        dict[property.Name] = property.Value.ToString();
                        break;
                }
            }
            return dict;
        }

        /// <summary>
        /// Chuyển đổi JsonElement thành List
        /// </summary>
        /// <param name="element">JsonElement cần chuyển đổi</param>
        /// <returns>List chứa thông tin từ JsonElement</returns>
        public List<object> ConvertJsonElementToList(JsonElement element)
        {
            var list = new List<object>();
            if (element.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var item in element.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.Object:
                        list.Add(ConvertJsonElementToDict(item));
                        break;
                    case JsonValueKind.Array:
                        list.Add(ConvertJsonElementToList(item));
                        break;
                    case JsonValueKind.String:
                        list.Add(item.GetString());
                        break;
                    case JsonValueKind.Number:
                        if (item.TryGetInt32(out int intValue))
                        {
                            list.Add(intValue);
                        }
                        else if (item.TryGetInt64(out long longValue))
                        {
                            list.Add(longValue);
                        }
                        else
                        {
                            list.Add(item.GetDouble());
                        }
                        break;
                    case JsonValueKind.True:
                        list.Add(true);
                        break;
                    case JsonValueKind.False:
                        list.Add(false);
                        break;
                    case JsonValueKind.Null:
                        list.Add(null);
                        break;
                    default:
                        list.Add(item.ToString());
                        break;
                }
            }
            return list;
        }
    }
}
