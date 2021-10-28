using EasyTeams.Common.Config;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EasyTeams.Common
{
    public class FunctionAppProxy
    {
        public SystemSettings Settings { get; set; }
        public FunctionAppProxy(SystemSettings settings)
        {
            this.Settings = settings;
        }


        /// <summary>
        /// Send a payload to the configured funcion-app via POST
        /// </summary>
        public async Task PostDataToFunctionApp(string requestContent, bool throwExceptionIfFuncionAppCallFails)
        {
            using (var client = new HttpClient())
            {
                // Add functions key if defined in configuration
                if (!string.IsNullOrEmpty(Settings.FunctionAppKey))
                {
                    client.DefaultRequestHeaders.Add("x-functions-key", Settings.FunctionAppKey);
                }

                // POST request to functions app to create meetings
                var response = await client.PostAsync(
                    Settings.NewEventCreationURL,
                     new StringContent(requestContent, System.Text.Encoding.UTF8, "application/json"));
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException ex)
                {

                    string requestBody = await response.Content.ReadAsStringAsync();
                    string msg = $"Could not submit meeting request to function app @ {Settings.NewEventCreationURL}.";
                    if (throwExceptionIfFuncionAppCallFails)
                    {
                        throw new ApplicationException(msg, ex);
                    }
                    else
                    {
                        Console.WriteLine($"ERROR: {msg}");
                    }
                }
            }
        }

    }
}
