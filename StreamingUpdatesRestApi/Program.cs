using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OSIsoft.Data;
using OSIsoft.Data.Reflection;
using OSIsoft.Identity;

namespace StreamingUpdatesRestApi
{
    public static class Program
    {
        private static IConfiguration _configuration;
        private static Exception _toThrow;

        public static void Main()
        {
            Console.WriteLine("Beginning sample DotNet application for AVEVA DataHub StreamingUpdates");
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task<bool> MainAsync(bool test = false)
        {
            #region Setup
            _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

            // ==== Client constants ====
            string tenantId = _configuration["TenantId"];
            string namespaceId = _configuration["NamespaceId"];
            string resource = _configuration["Resource"];
            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];
            string apiVersion = _configuration["ApiVersion"];

            // ==== Ids ====
            const string TypeId = "SimpleSdsTypeId";
            string signupId = "";

            // ==== Names ====
            const string SignupName = "signupSample";
            const string StreamNamePrefix = "stream_";
            const string NewStreamName = "newStream";
            const string GetUpdatesHeader = "Get-Updates";

            // === Change this to desired number of streams to create and update ===
            const int NumOfStreamsToCreate = 3;
            const int NumOfStreamsToUpdate = 3;
            #endregion

            // Step 1
            // Obtain authentication handler for ADH using Client-credential clients
            // Create Sds communication services
            #region Step1
            Console.WriteLine("Step 1: Obtain authentication handler and create Sds communication services");
            AuthenticationHandler authenticationHandler = new (new Uri(resource), clientId, clientSecret);
            SdsService sdsService = new (new Uri(resource), authenticationHandler);
            ISdsMetadataService metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
            ISdsDataService dataService = sdsService.GetDataService(tenantId, namespaceId);
            Console.WriteLine();
            #endregion

            using (HttpClient httpClient = new (authenticationHandler))
            {
                try
                {
                    // Step 2
                    // Create a simple SDS Type
                    #region Step2
                    Console.WriteLine("Step 2: Creating a simple SDS Type");
                    SdsType type = SdsTypeBuilder.CreateSdsType<SdsSimpleType>();
                    type.Id = TypeId;
                    type = await metadataService.GetOrCreateTypeAsync(type).ConfigureAwait(false);
                    Console.WriteLine();
                    #endregion

                    // Step 3
                    // Create SDS Streams and populate list of stream Ids for creating signup
                    #region Step3
                    Console.WriteLine("Step 3: Creating SDS Streams and populate list of stream Ids");
                    List<string> streamIdList = new List<string>();

                    for (int i = 0; i < NumOfStreamsToCreate; i++)
                    {
                        SdsStream sdsStream = new SdsStream()
                        {
                            Id = StreamNamePrefix + i,
                            Name = StreamNamePrefix + i,
                            TypeId = type.Id,
                            Description = $"Stream {i} for ADH Streaming Updates",
                        };

                        sdsStream = await metadataService.GetOrCreateStreamAsync(sdsStream).ConfigureAwait(false);
                        streamIdList.Add(sdsStream.Id);
                    }

                    Console.WriteLine();
                    #endregion

                    // STREAMING UPDATES:
                    // Step 4
                    // Create an ADH Signup against the created resources (streams)
                    #region Step4
                    Console.WriteLine("Step 4: Creating Signup");
                    CreateSignupInput signupToCreate = new CreateSignupInput()
                    {
                        Name = SignupName,
                        ResourceType = ResourceType.Stream,
                        ResourceIds = streamIdList,
                    };

                    using StringContent signupToCreateString = new (JsonSerializer.Serialize(signupToCreate));
                    signupToCreateString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups", UriKind.Absolute), signupToCreateString).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Get Signup Id from HttpResponse
                    Signup signup = JsonSerializer.Deserialize<Signup>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    
                    if (signup != null)
                    {
                        signupId = signup.Id;
                        Console.WriteLine($"Signup {signupId} has been created and is {signup?.SignupState}");
                    }
                    else
                    {
                        throw new NullReferenceException();
                    }

                    Console.WriteLine();
                    #endregion

                    // 5 second delay to allow signup to be ready to activate
                    Thread.Sleep(5000);

                    // Step 5
                    // Make an API request to GetSignup to activate the signup
                    #region Step5
                    Console.WriteLine($"Step 5: Activating signup");
                    response = await httpClient.GetAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}", UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Check signup state is active
                    signup = JsonSerializer.Deserialize<Signup>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    Console.WriteLine($"Signup is now {signup?.SignupState}");

                    // Get Bookmark for GetUpdates Request from Headers
                    string getUpdates = response.Headers.TryGetValues(GetUpdatesHeader, out var values) ? values.FirstOrDefault() : null;
                    Console.WriteLine();
                    #endregion

                    // Step 6
                    // Make an API request to GetSignupResources to view the signup's accessible and inaccessible resources
                    #region Step6
                    Console.WriteLine("Step 6: Get Signup Resources");

                    response = await httpClient.GetAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}/resources", UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);
                    SignupResourceIds resources = JsonSerializer.Deserialize<SignupResourceIds>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));

                    if (resources != null)
                    {
                        foreach (var resourceId in resources.AccessibleResources)
                        {
                            Console.WriteLine($"Accessible Resource: {resourceId}");
                        }

                        foreach (var resourceId in resources.InaccessibleResources)
                        {
                            Console.WriteLine($"Inaccessible Resource: {resourceId}");
                        }
                    }

                    Console.WriteLine();
                    #endregion

                    // Step 7
                    // Make updates to the Streams (post data to stream)
                    #region Step7
                    Console.WriteLine("Step 7: Making updates to previously created streams");

                    for (int i = 0; i < NumOfStreamsToUpdate; i++)
                    {
                        var streamId = StreamNamePrefix + i;
                        await dataService.InsertValuesAsync(streamId, GetData()).ConfigureAwait(false);
                    }

                    Console.WriteLine();
                    #endregion

                    // 20 second delay to catch up to updates
                    Console.WriteLine("Waiting for updates to process\n");
                    Thread.Sleep(20000);

                    // Step 8
                    // Make an API request to GetUpdates and ensure that data updates are received
                    #region Step8
                    Console.WriteLine("Step 8: Get Updates");

                    if (!string.IsNullOrEmpty(getUpdates))
                    {
                        response = await httpClient.GetAsync(new Uri(getUpdates, UriKind.Absolute)).ConfigureAwait(false);
                    }

                    CheckIfResponseWasSuccessful(response);
                    DataUpdate dataUpdate = JsonSerializer.Deserialize<DataUpdate>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                                                                                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dataUpdate != null)
                    {
                        foreach (var update in dataUpdate.Data)
                        {
                            Console.WriteLine($"Update: {update.ResourceId} {update.Operation}");

                            foreach (var updateEvent in update.Events)
                            {
                                Console.WriteLine($"\tTime: {updateEvent.Time} Value: {updateEvent.Value}");
                            }
                        }
                    }

                    Console.WriteLine();
                    #endregion

                    // Step 9
                    // Create a new SDS Stream and make an API Request to UpdateSignupResources to add the stream to signup
                    #region Step9
                    SdsStream newSdsStream = new SdsStream()
                    {
                        Id = NewStreamName,
                        Name = NewStreamName,
                        TypeId = type.Id,
                        Description = $"New Stream for ADH Streaming Updates",
                    };

                    newSdsStream = await metadataService.GetOrCreateStreamAsync(newSdsStream).ConfigureAwait(false);

                    Console.WriteLine("Step 9: Updating Signup Resources");
                    SignupResourcesInput signupToUpdate = new SignupResourcesInput()
                    {
                        ResourcesToAdd = new List<string>() { newSdsStream.Id },
                        ResourcesToRemove = new List<string>() { },
                    };

                    using StringContent signupToUpdateString = new (JsonSerializer.Serialize(signupToUpdate));
                    signupToUpdateString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await httpClient.PostAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}/resources", UriKind.Absolute), signupToUpdateString).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);
                    Console.WriteLine();
                    #endregion

                    // Step 10
                    // Make an API request to GetSignupResources to view signup with updated resources
                    #region Step10
                    Console.WriteLine("Step 10: Get Signup Resources");
                    response = await httpClient.GetAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}/resources", UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);
                    resources = JsonSerializer.Deserialize<SignupResourceIds>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));

                    if (resources != null)
                    {
                        foreach (var resourceId in resources.AccessibleResources)
                        {
                            Console.WriteLine($"Accessible Resource: {resourceId}");
                        }

                        foreach (var resourceId in resources.InaccessibleResources)
                        {
                            Console.WriteLine($"Inaccessible Resource: {resourceId}");
                        }
                    }

                    Console.WriteLine();
                    #endregion
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                    _toThrow = ex;
                    throw;
                }
                finally
                {
                    // Step 11
                    // Cleanup Resources
                    #region Step11
                    Console.WriteLine("Step 11: Cleaning Up");

                    Console.WriteLine($"Deleting ADH Signup with id {signupId}");
                    RunInTryCatch(httpClient.DeleteAsync, $"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}");

                    if (metadataService != null)
                    {
                        for (int i = 0; i < NumOfStreamsToCreate; i++)
                        {
                            Console.WriteLine($"Deleting {StreamNamePrefix + i}");
                            RunInTryCatch(metadataService.DeleteStreamAsync, StreamNamePrefix + i);
                        }

                        Console.WriteLine($"Deleting {NewStreamName}.");
                        RunInTryCatch(metadataService.DeleteStreamAsync, NewStreamName);
                        Console.WriteLine("Deleting Type.");
                        RunInTryCatch(metadataService.DeleteTypeAsync, TypeId);
                    }
                    #endregion
                }
            }

            if (test && _toThrow != null)
                throw _toThrow;
            return _toThrow == null;
        }

        /// <summary>
        /// Use this to run a method that you don't want to stop the program if there is an error
        /// </summary>
        /// <param name="methodToRun">The method to run.</param>
        /// <param name="value">The value to put into the method to run</param>
        private static void RunInTryCatch(Func<string, Task> methodToRun, string value)
        {
            try
            {
                methodToRun(value).Wait(10000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got error in {methodToRun.Method.Name} with value {value} but continued on:" + ex.Message);
                _toThrow ??= ex;
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
            List<SdsSimpleType> data = new ()
            {
                new SdsSimpleType { Time = DateTime.Now, Value = 10 },
                new SdsSimpleType { Time = DateTime.Now, Value = 20 },
                new SdsSimpleType { Time = DateTime.Now, Value = 30 },
            };

            return data;
        }
    }
}
