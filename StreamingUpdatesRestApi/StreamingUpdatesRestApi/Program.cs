using Microsoft.Extensions.Configuration;
using OSIsoft.Data;
using OSIsoft.Data.Reflection;
using OSIsoft.Identity;
using System.Net.Http.Headers;
using System.Text.Json;

namespace StreamingUpdatesRestApi
{
    public static class Program
    {
        private static IConfiguration _configuration;

        public static void Main()
        {
            Console.WriteLine("Beginnging sample DotNet application for AVEVA DataHub StreamingUpdates.");
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task<bool> MainAsync()
        {
            #region Test Data Settings
            string typeId = "Simple Sds Time Value Type";
            string streamNamePrefix = "stream_";
            string signupName = "PLACEHOLDER_SIGNUP_NAME";

            // Change this to desired number of streams to create.
            int numOfStreamsToCreate = 3;
            #endregion

            try
            {
                // Make a copy of appsettings.placeholder.json and replace credentials prior to running.
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
                // Obtain authentication handler for ADH using Client-credential clients.
                // Create Sds communication services.
                #region step1
                AuthenticationHandler authenticationHandler = new(uriResource, clientId, clientSecret);
                SdsService sdsService = new(new Uri(resource), authenticationHandler);
                ISdsMetadataService metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
                ISdsDataService dataService = sdsService.GetDataService(tenantId, namespaceId);
                #endregion

                using (HttpClient httpClient = new(authenticationHandler) { BaseAddress = new Uri(resource) })
                {
                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

                    // Step 2
                    // Create a simple SDS Type.
                    #region step2
                    SdsType type = SdsTypeBuilder.CreateSdsType<SdsSimpleType>();
                    type.Id = typeId;
                    type = await metadataService.GetOrCreateTypeAsync(type).ConfigureAwait(false);
                    #endregion

                    // Step 3
                    // Create a SDS Streams and populate list of stream Ids for creating signup.
                    #region step3
                    List<string> streamIdList = new List<string>();

                    for (int i = 0; i < numOfStreamsToCreate; i++)
                    {
                        SdsStream sdsStream = new SdsStream()
                        {
                            Id = streamNamePrefix + i,
                            Name = streamNamePrefix + i,
                            TypeId = type.Id,
                            Description = "Stream one for ADH Streaming Updates"
                        };

                        sdsStream = await metadataService.GetOrCreateStreamAsync(sdsStream).ConfigureAwait(false);
                        streamIdList.Add(sdsStream.Id);
                    }
                    #endregion

                    // STREAMING UPDATES:
                    // Step 4
                    // Create an ADH Signup against the created resources (streams).
                    #region step4
                    CreateSignupInput signupToCreate = new CreateSignupInput()
                    {
                        Name = signupName,
                        ResourceType = ResourceType.Stream,
                        ResourceIds = streamIdList,
                    };

                    using StringContent signupToCreateString = new(JsonSerializer.Serialize(signupToCreate));
                    signupToCreateString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(
                        new Uri($"/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups", UriKind.Relative), 
                        signupToCreateString).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Get Signup Id from HttpResponse.

                    #endregion

                    // Step 5
                    // Make an API request to GetSignup to activate the signup.


                    // Step 6
                    // Make updates to the Streams (post data to stream).

                    // Step 7
                    // Make an API request to GetUpdates and ensure that data updates are received.

                    // Step 8
                    // Create a new SDS Stream and update Signup resources to include the new stream.

                    // Step 9
                    // Make an API request to GetSignup with new updated resources.

                    // Step 10
                    // Make an API request to GetUpdates and ensure that data updates received.
                }
                return true;

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static void CheckIfResponseWasSuccessful(HttpResponseMessage response)
        {
            // If support is needed please know the Operation-ID header information for support purposes (it is included in the exception below automatically too)
            // string operationId = response.Headers.GetValues("Operation-Id").First();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(response.ToString());
            }
        }
    }
}