using System.Text.Json;

namespace StreamingUpdatesRestApi
{
    public class DataUpdate
    {
        public string Bookmark { get; set; }
        
        public IEnumerable<JsonElement> Data { get; set; }
    }
}
