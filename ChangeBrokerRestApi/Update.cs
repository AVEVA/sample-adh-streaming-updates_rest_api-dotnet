using System.Text.Json;

namespace ChangeBrokerRestApi
{
    public class Update
    {
        public string ResourceId { get; set; }
        public string Operation { get; set; }
        public IEnumerable<JsonElement> Events { get; set; }
    }
}
