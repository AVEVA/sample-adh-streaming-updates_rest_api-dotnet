﻿using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using OSIsoft.Data;
using OSIsoft.Data.Reflection;
using OSIsoft.Identity;
using static System.Text.Json.JsonElement;

namespace StreamingUpdatesRestApi
{
    public static class Program
    {
        // stream id and name prefixes
        private const string SimpleStreamPrefix = "simpleStream_";
        private const string WeatherDataStreamPrefix = "weatherDataStream_";

        // Get Updates properties
        private const string Data = "data";
        private const string Events = "events";
        private const string Operation = "operation";
        private const string ResourceId = "resourceId";

        private static IConfiguration _configuration;
        private static Exception _toThrow;
        private static JsonSerializerOptions _apiJsonOptions;
        private static JsonSerializerOptions _dataJsonOptions;

        public static void Main()
        {
            Console.WriteLine("Beginning sample DotNet application for AVEVA DataHub StreamingUpdates");
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task<bool> MainAsync(bool test = false)
        {
            #region Setup
            // streaming updates API serialization options
            _apiJsonOptions = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            _apiJsonOptions.Converters.Add(new JsonStringEnumConverter());

            // data serialization options
            _dataJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

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
            const string SimpleTypeId = "SimpleSdsTypeIdDotNet";
            const string WeatherDataTypeId = "WeatherDataTypeIdDotNet";
            string signupId = "";

            // ==== Names ====
            const string SignupName = "signupSample";
            
            // === Change these values to modify the number of streams to create ===
            const int SimpleStreamsToCreate = 2;
            const int WeatherDataStreamsToCreate = 1;

            List<string> simpleStreamIdList = new List<string>();
            List<string> weatherDataStreamIdList = new List<string>();
            #endregion

            // Step 1
            // Obtain authentication handler for ADH using Client-credential clients
            // Create SDS communication services
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
                    // Create SDS Types
                    #region Step2
                    Console.WriteLine("Step 2: Creating SDS Types");

                    // Create SdsSimpleType
                    SdsType type = SdsTypeBuilder.CreateSdsType<SdsSimpleType>();
                    type.Id = SimpleTypeId;
                    type = await metadataService.GetOrCreateTypeAsync(type).ConfigureAwait(false);

                    // Create WeatherDataType
                    type = SdsTypeBuilder.CreateSdsType<WeatherDataType>();
                    type.Id = WeatherDataTypeId;
                    type = await metadataService.GetOrCreateTypeAsync(type).ConfigureAwait(false);

                    Console.WriteLine();
                    #endregion

                    // Step 3
                    // Create SDS Streams and populate list of stream Ids for creating signup
                    #region Step3
                    Console.WriteLine("Step 3: Creating SDS Streams and populate list of stream Ids");
                    List<string> streamsToAdd = new ();

                    // Create streams for SdsSimpleType
                    for (int i = 0; i < SimpleStreamsToCreate; i++)
                    {
                        SdsStream sdsStream = new SdsStream()
                        {
                            Id = SimpleStreamPrefix + i,
                            Name = SimpleStreamPrefix + i,
                            TypeId = SimpleTypeId,
                            Description = $"Simple Stream for ADH Streaming Updates",
                        };

                        sdsStream = await metadataService.GetOrCreateStreamAsync(sdsStream).ConfigureAwait(false);
                        simpleStreamIdList.Add(sdsStream.Id);
                    }

                    streamsToAdd.AddRange(simpleStreamIdList);

                    // Create streams for WeatherDataType
                    for (int i = 0; i < WeatherDataStreamsToCreate; i++)
                    {
                        SdsStream weatherDataStream = new SdsStream()
                        {
                            Id = WeatherDataStreamPrefix + i,
                            Name = WeatherDataStreamPrefix + i,
                            TypeId = WeatherDataTypeId,
                            Description = "Weather Data Stream for ADH Streaming Updates",
                        };

                        weatherDataStream = await metadataService.GetOrCreateStreamAsync(weatherDataStream).ConfigureAwait(false);
                        weatherDataStreamIdList.Add(weatherDataStream.Id);
                    }

                    streamsToAdd.AddRange(weatherDataStreamIdList);

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
                        ResourceIds = streamsToAdd,
                    };

                    using StringContent signupToCreateString = new (JsonSerializer.Serialize(signupToCreate));
                    signupToCreateString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups", UriKind.Absolute), signupToCreateString).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Get Signup Id from HttpResponse
                    Signup signup = JsonSerializer.Deserialize<Signup>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), _apiJsonOptions);
                    signupId = signup?.Id;
                    Console.WriteLine($"Signup {signupId} has been created and is {signup?.SignupState}");

                    Console.WriteLine();
                    #endregion

                    // 1 second delay to allow signup to be ready to activate
                    Thread.Sleep(1000);

                    // Step 5
                    // Make an API request to GetSignup to activate the signup
                    #region Step5
                    Console.WriteLine($"Step 5: Activating signup");
                    response = await httpClient.GetAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}", UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Check signup state is active
                    var signupWithBookmark = JsonSerializer.Deserialize<SignupWithBookmark>(await response.Content.ReadAsStringAsync().ConfigureAwait(false), _apiJsonOptions);
                    Console.WriteLine($"Signup is now {signupWithBookmark?.SignupState}");

                    // If the signup is not yet in the active state, then try allowing more time for the signup to activate.
                    if (signupWithBookmark?.SignupState != SignupState.Active)
                    {
                        Console.WriteLine("A bookmark can only be obtained from an active signup.");
                        return false;
                    }

                    string bookmark = signupWithBookmark?.Bookmark;
                    Console.WriteLine();
                    #endregion

                    // Step 6
                    // Make an API request to GetSignupResources to view the signup's accessible and inaccessible resources
                    #region Step6
                    Console.WriteLine("Step 6: Get Signup Resources");

                    response = await httpClient.GetAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}/resources", UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    var signupResources = JsonSerializer.Deserialize<SignupResources>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), _apiJsonOptions);

                    if (signupResources != null)
                    {
                        foreach (var signupResource in signupResources.Resources)
                        {
                            Console.WriteLine($"Resource: {signupResource.ResourceId}, Accessible: {signupResource.IsAccessible}");
                        }
                    }

                    Console.WriteLine();
                    #endregion

                    // Step 7
                    // Make updates to the Streams (post data to stream)
                    #region Step7
                    Console.WriteLine("Step 7: Writing data to the streams");
                    
                    // populate simple streams
                    foreach (var streamId in simpleStreamIdList)
                    {
                        await dataService.InsertValuesAsync(streamId, GetSimpleData()).ConfigureAwait(false);
                    }

                    // populate weather data streams
                    foreach (var streamId in weatherDataStreamIdList)
                    {
                        await dataService.InsertValuesAsync(streamId, GetWeatherData()).ConfigureAwait(false);
                    }

                    // 20 second delay to catch up to updates. Continue polling if desired number of updates are not available or increase wait time.
                    Console.WriteLine("Waiting for updates to process\n");
                    Thread.Sleep(20000);
                    #endregion

                    // Step 8
                    // Make an API request to GetUpdates and ensure that data updates are received
                    #region Step8
                    Console.WriteLine("Step 8: Get Updates");

                    response = await httpClient.GetAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}/updates?bookmark={bookmark}", UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    DataUpdate dataUpdate = JsonSerializer.Deserialize<DataUpdate>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), _dataJsonOptions) !;

                    // Note:The sequence in which update operations, (e.g. resourceId: Stream A, operation: Insert, events: [...]) are returned may differ from
                    // the order in which they are written. The order of data events within a stream, (e.g. Timestamp 12:00:00, Value: 23) is preserved.
                    foreach (JsonElement update in dataUpdate.Data)
                    {
                        if (IsSdsSimpleType(update))
                        {
                            ProcessSimpleStreamUpdate(update);
                        }
                        else
                        {
                            ProcessWeatherDataUpdate(update);
                        }

                        Console.WriteLine();
                    }
                    
                    #endregion

                    // Step 9
                    // Create a new SDS Stream and make an API Request to UpdateSignupResources to add the stream to signup
                    #region Step9
                    SdsStream newSdsStream = new SdsStream()
                    {
                        Id = WeatherDataStreamPrefix + "New",
                        Name = WeatherDataStreamPrefix + "New",
                        TypeId = WeatherDataTypeId,
                        Description = $"New Weather Data Stream for ADH Streaming Updates",
                    };

                    newSdsStream = await metadataService.GetOrCreateStreamAsync(newSdsStream).ConfigureAwait(false);
                    weatherDataStreamIdList.Add(newSdsStream.Id);

                    Console.WriteLine("Step 9: Updating Signup Resources with a new Weather Data Stream");
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
                    
                    signupResources = JsonSerializer.Deserialize<SignupResources>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), _apiJsonOptions);

                    if (signupResources != null)
                    {
                        foreach (var signupResource in signupResources.Resources)
                        {
                            Console.WriteLine($"Resource: {signupResource.ResourceId}, Accessible: {signupResource.IsAccessible}");
                        }
                    }

                    // Step 11
                    // Populate streams using non-Insert operations
                    Console.WriteLine("Step 11: Populate streams with Replace, Update, Remove and RemoveWindow operations");

                    Console.WriteLine();
                    #endregion

                    // Step 12
                    // Make a new API request to GetUpdates using the bookmark obtained from the previous GetUpdates response to
                    // demonstrate update retrieval  other operation types (for example, Replace, Update, Remove and RemoveWindow).
                    #region Step12
                    Console.WriteLine("Step 11: Get Updates");

                    response = await httpClient.GetAsync(new Uri($"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}/updates?bookmark={dataUpdate.Bookmark}", UriKind.Absolute)).ConfigureAwait(false);

                    CheckIfResponseWasSuccessful(response);

                    using JsonDocument jsonDocumentNext = JsonSerializer.Deserialize<JsonDocument>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), _dataJsonOptions) !;
                    var newUpdates = jsonDocumentNext.RootElement.GetProperty(Data).EnumerateArray();

                    // The response will not contain any data because no new events have been written to the streams in the signup.
                    Console.WriteLine($"Updates Count: {newUpdates.Count()}");

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
                    // Step 12
                    // Cleanup Resources
                    #region Step12
                    Console.WriteLine("Step 12: Cleaning Up");

                    Console.WriteLine($"Deleting ADH Signup with id {signupId}");
                    RunInTryCatch(httpClient.DeleteAsync, $"{resource}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}");

                    if (metadataService != null)
                    {
                        foreach (var streamId in simpleStreamIdList)
                        {
                            Console.WriteLine($"Deleting {streamId}");
                            RunInTryCatch(metadataService.DeleteStreamAsync, streamId);
                        }

                        foreach (var streamId in weatherDataStreamIdList)
                        {
                            Console.WriteLine($"Deleting {streamId}");
                            RunInTryCatch(metadataService.DeleteStreamAsync, streamId);
                        }

                        Console.WriteLine($"Deleting {nameof(SdsSimpleType)}.");
                        RunInTryCatch(metadataService.DeleteTypeAsync, SimpleTypeId);

                        Console.WriteLine($"Deleting {nameof(WeatherDataType)}.");
                        RunInTryCatch(metadataService.DeleteTypeAsync, WeatherDataTypeId);
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

        private static IList<SdsSimpleType> GetSimpleData()
        {
            List<SdsSimpleType> data = new ()
            {
                new SdsSimpleType { Timestamp = DateTime.Now, Value = 10 },
                new SdsSimpleType { Timestamp = DateTime.Now, Value = 20 },
                new SdsSimpleType { Timestamp = DateTime.Now, Value = 30 },
            };

            return data;
        }

        private static IList<WeatherDataType> GetWeatherData()
        {
            List<WeatherDataType> data = new ()
            {
                new WeatherDataType { Timestamp = DateTime.Now, Humidity = 40.0, Temperature = 25.0 },
                new WeatherDataType { Timestamp = DateTime.Now, Humidity = 40.1, Temperature = 25.1 },
            };

            return data;
        }

        private static bool IsSdsSimpleType(JsonElement update)
        {
            string id = update.GetProperty(ResourceId).GetString() ?? string.Empty;
            return id.StartsWith(SimpleStreamPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static void ProcessSimpleStreamUpdate(JsonElement update)
        {
            string id = update.GetProperty(ResourceId).GetString();
            string operation = update.GetProperty(Operation).GetString();
            ArrayEnumerator events = update.GetProperty(Events).EnumerateArray();

            Console.WriteLine($"id: {id}");
            Console.WriteLine($"operation: {operation}");
            foreach (JsonElement jsonDataEvent in events)
            {
                SdsSimpleType dataEvent = JsonSerializer.Deserialize<SdsSimpleType>(jsonDataEvent);
                Console.WriteLine($"\tTimestamp: {dataEvent?.Timestamp}, Value: {dataEvent?.Value}");
            }
        }

        private static void ProcessWeatherDataUpdate(JsonElement update)
        {
            string id = update.GetProperty(ResourceId).GetString();
            string operation = update.GetProperty(Operation).GetString();
            ArrayEnumerator events = update.GetProperty(Events).EnumerateArray();

            Console.WriteLine($"id: {id}");
            Console.WriteLine($"operation: {operation}");
            foreach (JsonElement jsonDataEvent in events)
            {
                WeatherDataType dataEvent = JsonSerializer.Deserialize<WeatherDataType>(jsonDataEvent);
                Console.WriteLine($"\tTimestamp: {dataEvent?.Timestamp}, Humidity: {dataEvent?.Humidity}, Temperature: {dataEvent?.Temperature}");
            }
        }
    }
}
