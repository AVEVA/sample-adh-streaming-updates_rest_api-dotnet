using Microsoft.Extensions.Configuration;
using OSIsoft.Data;
using OSIsoft.Identity;

namespace StreamingUpdatesRestApi
{
    public static class Program
    {
        private static IConfiguration _configuration;

        public static void Main()
        {

            _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.test.json", optional: true)
                    .Build();

            string tenantId = _configuration["TenantId"];
            string namespaceId = _configuration["NamespaceId"];
            string resource = _configuration["Resource"];
            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];

            Uri uriResource = new(resource);
            AuthenticationHandler authenticationHandler = new(uriResource, clientId, clientSecret);
            SdsService sdsService = new(new Uri(resource), authenticationHandler);
        }
    }
}


