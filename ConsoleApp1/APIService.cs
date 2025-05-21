/*using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace YourNamespace
{
    public class ApiService
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<string> UpdateFeatureStatusAsync(string vin, string token, string activationStatus, string shortName)
        {
            string url = $"https://odft.query.api.dvb.corpinter.net/api/featurestatus?vin={vin}";

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var content = new StringContent(
                $"{{\"activationStatus\": \"{activationStatus}\", \"shortName\": \"{shortName}\"}}",
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
    }
}*/