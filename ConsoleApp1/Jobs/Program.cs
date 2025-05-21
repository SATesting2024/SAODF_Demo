

//CORRECT CODE WITH TOKEN AND FEATURE STATUS and PUT command
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Runtime.Remoting.Messaging;
using System.Collections;
using System.Linq;
using ET.FW.Core.Messaging;
using WinSCP;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json;

//using ConsoleApp1.Jobs.DiagNTG7;


namespace ConsoleApp1
{

    public class ActivationState
    {
        public string active { get; set; }
        public bool expires { get; set; }
        //public DateTime? expirationDate { get; set; }
    }
    public class Feature
    {
        public string shortName { get; set; }
        public string activationStatus { get; set; }

    }
    public class Feature2
    {
        public string name { get; set; }
        public ActivationState ActivationState { get; set; }
    }
    public class VehicleStatus
    {
        public string connectedMeid { get; set; }
        public string lastOdfSync { get; set; }
        public List<Feature> features { get; set; }
    }
    public class VehicleStatus2
    {
        public string connectedMeid { get; set; }
        public string lastOdfSync { get; set; }
        public List<Feature2> features2 { get; set; }
    }

    public class TokenService
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<string> GenerateTokenAsync(string username, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://ssoalpha.dvb.corpinter.net/v1/token");

            // Encode the username and password to Base64
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Add("Cookie", "csrf-qnvxej6yqkaf5bq3gsayn2nnm=");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "openid profile groups audience:server:client_id:DAIVBADM_MICTM_EMEA_PROD_01813")
            });

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var token = JObject.Parse(responseBody)["access_token"].ToString();
            return token;
        }
    }

    public class GetStatus
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> GetFeatureStatusAsync(string vin, string shortname, string token)
        {
            string url = $"https://odft.query.api.dvb.corpinter.net/featurestatus/v2?vin={vin}&shortname={shortname}";

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.GetAsync(url);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                Console.WriteLine($"Status code: {response.StatusCode}");
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error response: {errorResponse}");
                throw;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
    }

    public class FeatureStatusUpdater
    {
        public async Task UpdateFeatureStatus(string vin, string authorization, string activationStatus, string shortName)
        {
            var client = new HttpClient();
           // var request = new HttpRequestMessage(HttpMethod.Put, $"https://odft.query.api.dvb.corpinter.net/featurestatus?vin={vin}");
            var request = new HttpRequestMessage(HttpMethod.Put, $"https://odft.query.api.dvb.corpinter.net/api/featurestatus?vin={vin}");

            request.Headers.Add("accept", "application/json");
            request.Headers.Add("Authorization", $"Bearer {authorization}");
            

            var json = $"{{\"activationStatus\": \"{activationStatus}\",\"shortName\": \"{shortName}\"}}";
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }


    //to get status of all subsystems and save it to JSON a and change the status of subsystems using PUT all 
    public class ODFToolClient
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> GetVehicleStatusAsync(string vin, string token)
        {
            string url = $"https://odft.query.api.dvb.corpinter.net/api/getvehiclestatus?vin={vin}";

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            HttpResponseMessage response = await client.GetAsync(url);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                Console.WriteLine($"Status code: {response.StatusCode}");
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error response: {errorResponse}");
                throw;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;

        }

        public static async Task SaveJsonToFileAsync(string jsonString, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                await writer.WriteAsync(jsonString);
            }
        }

        public static async Task SaveFilteredJsonToFileAsync(string jsonString, string filePath, params string[] parameters)
        {
            // Deserialize the JSON string into a VehicleStatus object
            var vehicleStatus = JsonSerializer.Deserialize<VehicleStatus>(jsonString);
            //var vehicleStatus2 = JsonSerializer.SerializeAsync(vehicleStatus);

            // Create a list to hold the selected fields
            var selectedFeatures = new List<Feature>();

            // Extract the required fields from each feature
            foreach (var feature in vehicleStatus.features)
            {
                selectedFeatures.Add(new Feature
                {
                    shortName = feature.shortName,

                    activationStatus = feature.activationStatus
                });
            }
            // Serialize the selected features to JSON
            string selectedFeaturesJson = JsonSerializer.Serialize(selectedFeatures, new JsonSerializerOptions { WriteIndented = true });
            // await File.WriteAllTextAsync(filePath1, selectedFeaturesJson);
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                await writer.WriteAsync(selectedFeaturesJson);
            }
            Console.WriteLine("Selected features stored successfully.");

        }

        public static async Task ExtractAndSaveJsonAsync(string jsonString, string outputFilePath)
        {
            var vehicleStatus = JsonSerializer.Deserialize<VehicleStatus2>(jsonString);
            var selectedFeature = new List<Feature2>();
            foreach (var features in vehicleStatus.features2)
            {
                selectedFeature.Add(new Feature2
                {
                    name = features.name,
                    ActivationState = new ActivationState
                    {
                        active = features.ActivationState.active,
                    }
                });
            }
            string selectedFeaturesJson = JsonSerializer.Serialize(selectedFeature, new JsonSerializerOptions { WriteIndented = true });
            using (StreamWriter writer = new StreamWriter(outputFilePath, false))
            {
                await writer.WriteAsync(selectedFeaturesJson);
            }
            Console.WriteLine("Updated features stored successfully.");

            // Parse the JSON string
            Console.WriteLine("Extracted JSON data has been saved to the file.");
        }

        public static async Task<string> UpdateMultipleFeatureStatus(string vin, string authorization, string jsonString, string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine("File not found.");
                return "File not found";
            }
            // Update jsonPayload based on the response
            // Serialize the updated JArray back into a JSON string 
            var jsonPayload = File.ReadAllText(jsonFilePath);
            JArray jsonArray = JArray.Parse(jsonPayload);

            foreach (var status in jsonArray)
            {
                if (status["activationStatus"] != null)
                {
                    string currentStatus = status["activationStatus"].ToString();
                    if (currentStatus == "Activated")
                    {
                        status["activationStatus"] = "DeactivationPending";
                    }
                    if (currentStatus == "Deactivated")
                    {
                        status["activationStatus"] = "ActivationPending";
                    }
                }
            }
            string updatedJsonData = jsonArray.ToString();

            // Write the updated JSON content back to the same file
            File.WriteAllText(jsonFilePath, updatedJsonData);
            //Console.WriteLine("JSON Content updated");
            //Console.WriteLine(updatedJsonData);
            jsonPayload = File.ReadAllText(jsonFilePath);

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"https://odft.query.api.dvb.corpinter.net/featurestatus/v2?vin={vin}");
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {authorization}");
                Console.WriteLine("Request started");

                // Send the HTTP request with jsonPayload as the content
                // var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                Console.WriteLine("Request in between");
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                // var response = await client.SendAsync(request.RequestUri, content);
                Console.WriteLine("Request ended");
                response.EnsureSuccessStatusCode();

                // Read the response content
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response received:");
                // Console.WriteLine(responseContent);
                string responseString = responseContent;

                Console.WriteLine("Response string:" + responseString);
                return responseString;
                //Save the filtered JSON content
                // SaveFilteredJsonToFileAsync(responseString, jsonFilePath2, "shortName", "activationStatus");


            }
        }
    }
    internal class Program
    {
        public async Task RebootBench()
        {
            SessionOptions sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Scp,
                HostName = "10.120.1.97",
                UserName = "root",
                PortNumber = 22,
                //SshHostKeyFingerprint = "2048 fb:b9:b4:f3:a2:f8:c8:f1:9d:2c:41:b4:ca:cd:4f:4e"
                //ssh - ed25519 255 Udb9epy + SNMR5BKaAulfcWicjjVKHw + FJw3CUv7Xt3o
                //ssh-rsa 2048 SHA256:lq8zpsCGZqKtHknBj6cCaWgwRcDxpo/Lu1xvFGLT3ZM
                // ssh-rsa 3072 K+Advbzd1qyzbV5Z6VallMVoth0eLCcNWcPvXjWJYMM
                //SshHostKeyFingerprint = "ssh-rsa 3072 K+Advbzd1qyzbV5Z6VallMVoth0eLCcNWcPvXjWJYMM"
                //SshHostKeyFingerprint = "ssh-rsa 1024 6EbPHt1B30putm4Euk8K46snFPyAMDv59NAtLF53ix8"
                //SshHostKeyFingerprint = "ssh-rsa 2048 xEK1KGPnvbVZSxT05NCWMfpU4xNxSpieKqbmKm3HXcs"
                SshHostKeyFingerprint = "ssh-rsa 1024 4J+Hj8g/jippXz2NXWk3RuadDgBpUw3tMyk78HOUAg0"
            };

            using (Session session = new Session())
            {
                // Connect
                session.Open(sessionOptions);
                session.ExecuteCommand("root");
                // Execute command
                session.ExecuteCommand("reset");
            }
            //Console.WriteLine("Bench Reboot");
        }

        static async Task Main(string[] args)
        {
            TokenService tokenService = new TokenService();
            var username = "F423ABAB-1A2F-49FE-B7B5-12FD7564460B";
            var password = "ZvEVp592w0o8t7Q3xk.1~chDTB4_L-X6";
            string token = await tokenService.GenerateTokenAsync(username, password);
            AutoResetEvent wait = new AutoResetEvent(false);
            string vin = "WYA1743441Z900349";
            string activationStatus = "ActivationPending";
            string shortName = "AMGRACE";

            Console.WriteLine("Token generation: "+ token);
            string token1 = token;
            //getting the status
            try
            {
                string result = await GetStatus.GetFeatureStatusAsync(vin, shortName, token1);
                Console.WriteLine();
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            //wait for 15 sec
            wait.WaitOne(15000);
            //change the status
            try
            {

                Console.WriteLine("Changing status of " + shortName);
                /*  var updater = new FeatureStatusUpdater();
                  await updater.UpdateFeatureStatus(vin, token1, activationStatus, shortName);*/

                // Console.WriteLine();
                string filePath1 = @"C:\\AutomationFiles\\UpdatedState.json";

                var jsonPayload = File.ReadAllText(filePath1);
                //Store updated status in another JSON file
                string result1 = await ODFToolClient.UpdateMultipleFeatureStatus(vin, token1, jsonPayload, filePath1);
                Console.WriteLine("Status is getting to the Pending state");
                // Console.WriteLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            //wait for 15 sec
            wait.WaitOne(15000);
            try
            {
                string result = await GetStatus.GetFeatureStatusAsync(vin, shortName, token1);
                Console.WriteLine();
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            wait.WaitOne(10000);
            Console.WriteLine("Rebooting the bench");
            //Rebbot to change the status
            Program program = new Program();
            await program.RebootBench();
            wait.WaitOne(20000);
            await program.RebootBench();
            wait.WaitOne(20000);
            await program.RebootBench();
            wait.WaitOne(20000);
            Console.WriteLine("Bench Reboot done, Headunit is up");
            wait.WaitOne(80000);
            //getting the status
            try
            {
                string result = await GetStatus.GetFeatureStatusAsync(vin, shortName, token1);
                Console.WriteLine();
                Console.WriteLine(result);
                Console.WriteLine(shortName + " Status is changed");
               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

   
            wait.WaitOne(10000);

        }         
    }
}



//code to try for the whole process

/*using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class APIClient
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task MakePutRequest(string vin, string activationStatus, string shortName, string bearerToken)
        {
            int maxRetries = 5;
            int delay = 1000; // Initial delay in milliseconds

            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // Construct the URL with the dynamic VIN parameter
                    var url = $"https://odft.query.api.dvb.corpinter.net/api/featurestatus?vin={vin}";

                    // Create the dynamic JSON body content for the request
                    var jsonContent = new
                    {
                        activationStatus = activationStatus,
                        shortName = shortName
                    };

                    // Serialize the object to JSON format
                    var jsonString = JsonSerializer.Serialize(jsonContent);

                    // Create the StringContent object (for JSON)
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // Add necessary headers
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

                    // Send the PUT request
                    var response = await client.PutAsync(url, content);

                    // Check if the response is successful
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Request successful");
                        var responseData = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Response: " + responseData);
                        return;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        Console.WriteLine("Too many requests. Retrying...");
                        await Task.Delay(delay);
                        delay *= 2; // Exponential backoff
                    }
                    else
                    {
                        Console.WriteLine("Request failed with status code: " + response.StatusCode);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return;
                }
            }

            Console.WriteLine("Max retries reached. Request failed.");
        }
    }
}
*/




