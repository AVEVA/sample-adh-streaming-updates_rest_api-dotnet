using System.Text.Json;

namespace ChangeBrokerRestApi
{
    public class DataUpdate
    {
        public string Bookmark { get; set; }
        
        public IEnumerable<Update> Data { get; set; }
    }
}
