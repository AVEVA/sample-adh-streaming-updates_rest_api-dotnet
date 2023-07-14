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
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task<bool> MainAsync()
        {
            // Make a copy of appsettings.placeholder.json and replace credentials prior to running
            _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName)
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.test.json", optional: true)
                    .Build();
            
            string tenantId = _configuration["TenantId"];
            string namespaceId = _configuration["NamespaceId"];
            string resource = _configuration["Resource"];
            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];

            Uri uriResource = new(resource);

            // Step 1
            // Obtain authentication handler for ADH using Client-credential clients
            AuthenticationHandler authenticationHandler = new(uriResource, clientId, clientSecret);
            SdsService sdsService = new(new Uri(resource), authenticationHandler);
            ISdsMetadataService metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
            var stream = await metadataService.GetStreamAsync("KangeeTest");

            Console.WriteLine(stream);

            return true;
        }
    }
}


