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
            Console.WriteLine("Beginning sample DotNet application for AVEVA DataHub StreamingUpdates.");
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            #region Test Data Settings
            string typeId = "Simple Sds Time Value Type";
            string streamNamePrefix = "stream_";

            // Change this to desired number of streams to create and update.
            int numOfStreamsToCreate = 3;
            int numOfStreamsToUpdate = 3;
            #endregion

            try
            {
                // Make a copy of appsettings.placeholder.json and replace credentials prior to running.
                #region Setup
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
                string signupName = _configuration["SignupName"];

                Uri uriResource = new(resource);
                #endregion

                // Step 1
                // Obtain authentication handler for ADH using Client-credential clients.
                // Create Sds communication services.
                #region Step1
                AuthenticationHandler authenticationHandler = new(uriResource, clientId, clientSecret);
                SdsService sdsService = new(new Uri(resource), authenticationHandler);
                ISdsMetadataService metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
                ISdsDataService dataService = sdsService.GetDataService(tenantId, namespaceId);
                #endregion

                using (HttpClient httpClient = new(authenticationHandler) { BaseAddress = new Uri(resource) })
                {
                    // Step 2
                    // Create a simple SDS Type.
                    #region Step2
                    SdsType type = SdsTypeBuilder.CreateSdsType<SdsSimpleType>();
                    type.Id = typeId;
                    type = await metadataService.GetOrCreateTypeAsync(type).ConfigureAwait(false);
                    #endregion

                    // Step 3
                    // Create SDS Streams and populate list of stream Ids for creating signup.
                    #region Step3
                    List<string> streamIdList = new List<string>();

                    for (int i = 0; i < numOfStreamsToCreate; i++)
                    {
                        SdsStream sdsStream = new SdsStream()
                        {
                            Id = streamNamePrefix + i,
                            Name = streamNamePrefix + i,
                            TypeId = type.Id,
                            Description = $"Stream one for ADH Streaming Updates"
                        };

                        sdsStream = await metadataService.GetOrCreateStreamAsync(sdsStream).ConfigureAwait(false);
                        streamIdList.Add(sdsStream.Id);
                    }
                    #endregion

                    // STREAMING UPDATES:
                    // Step 4
                    // Create an ADH Signup against the created resources (streams).
                    #region Step4
                    Console.WriteLine("Creating Signup.");
                    CreateSignupInput signupToCreate = new CreateSignupInput()
                    {
                        Name = signupName,
                        ResourceType = ResourceType.Stream,
                        ResourceIds = streamIdList,
                    };

                    using StringContent signupToCreateString = new(JsonSerializer.Serialize(signupToCreate));
                    signupToCreateString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(new Uri($"/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups", UriKind.Relative), signupToCreateString).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Get Signup Id from HttpResponse.
                    Signup? signup = JsonSerializer.Deserialize<Signup>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    Console.WriteLine($"Signup {signup?.Id} has been created.");
                    #endregion

                    // Step 5
                    // Make an API request to GetSignup to activate the signup.
                    #region Step5
                    Console.WriteLine($"Activating signup");
                    response = await httpClient.GetAsync(new Uri($"/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signup?.Id}", UriKind.Relative)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Check signup state is active.
                    signup = JsonSerializer.Deserialize<Signup>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    Console.WriteLine($"Signup is now {signup?.SignupState}.");
                    #endregion

                    // Step 6
                    // Make updates to the Streams (post data to stream).
                    #region Step6
                    for (int i = 0; i < numOfStreamsToUpdate; i++)
                    {
                        var streamId =  streamNamePrefix + i;
                        await dataService.InsertValuesAsync(streamId, GetData());
                    }
                    #endregion

                    // Step 7
                    // Make an API request to GetUpdates and ensure that data updates are received. (Parse)
                    #region Step7
                    #endregion

                    // Step 8
                    // Create a new SDS Stream and make an API Request to UpdateSignupResources to add the stream to signup.
                    #region Step8
                    SdsStream newSdsStream = new SdsStream()
                    {
                        Id = "newStream",
                        Name = "newStream",
                        TypeId = type.Id,
                        Description = $"New Stream for ADH Streaming Updates"
                    };

                    newSdsStream = await metadataService.GetOrCreateStreamAsync(newSdsStream).ConfigureAwait(false);

                    Console.WriteLine($"Updating Signup Resources.");
                    SignupResourcesInput signupToUpdate = new SignupResourcesInput()
                    {
                        ResourcesToAdd = new List<string>() { newSdsStream.Id }
                    };

                    using StringContent signupToUpdateString = new(JsonSerializer.Serialize(signupToCreate));
                    signupToUpdateString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await httpClient.PostAsync(new Uri($"/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups", UriKind.Relative), signupToUpdateString).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);
                    #endregion

                    // Step 9
                    // Make an API request to GetSignup to view signup with updated resources.
                    #region Step9
                    Console.WriteLine($"Get Signup.");
                    response = await httpClient.GetAsync(new Uri($"/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signup?.Id}", UriKind.Relative)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);
                    #endregion

                    // Step 10
                    // Make an API request to GetUpdates and ensure that data updates received. (Parse)
                    #region Step10
                    #endregion
                }
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

        private static IList<SdsSimpleType> GetData()
        {
            List<SdsSimpleType> data = new()
            {
                new SdsSimpleType {Time = DateTimeOffset.Now, Value = 10},
                new SdsSimpleType {Time = DateTimeOffset.Now, Value = 20},
                new SdsSimpleType {Time = DateTimeOffset.Now, Value = 30},
            };

            return data;
        }
    }
}