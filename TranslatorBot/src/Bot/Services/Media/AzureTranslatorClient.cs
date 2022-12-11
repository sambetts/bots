using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace TranslatorBot.Services.Media
{
    public class AzureTranslatorClient : IDisposable
    {
        private string _baseUrl;
        private string _key;
        private readonly string _region;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        public AzureTranslatorClient(string baseUrl, string key, string region, ILogger logger)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException($"'{nameof(baseUrl)}' cannot be null or empty.", nameof(baseUrl));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
            }

            _baseUrl = baseUrl;
            _key = key;
            _region = region;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task<TranslateResponse> TranslateAsync(string spokenText, string fromLanguage, string toLanguage)
        {
            using (var request = new HttpRequestMessage())
            {
                var route = $"/translate?api-version=3.0&from={fromLanguage}&to={toLanguage}";


                var body = new object[] { new { Text = spokenText } };
                var requestBody = JsonSerializer.Serialize(body);

                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(_baseUrl + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", _key);

                // location required if you're using a multi-service or regional (not global) resource.
                request.Headers.Add("Ocp-Apim-Subscription-Region", _region);

                // Send the request and get response.
                var response = await _httpClient.SendAsync(request);

                // Read response as a string.
                var result = await response.Content.ReadAsStringAsync();

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    _logger.LogError($"Got error {response.StatusCode} calling Azure service. Response body: {result}");
                    throw;
                }

                var responseObj = JsonSerializer.Deserialize<List<TranslateResponse>>(result);

                return responseObj.FirstOrDefault();
            }
        }
    }

    public class DetectedLanguage
    {
        [JsonPropertyName("language")]
        public string language { get; set; }

        [JsonPropertyName("score")]
        public double score { get; set; }
    }

    public class TranslateResponse
    {
        [JsonPropertyName("detectedLanguage")]
        public DetectedLanguage DetectedLanguage { get; set; }

        [JsonPropertyName("translations")]
        public List<Translation> Translations { get; set; }
    }

    public class Translation
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }
    }
}