using Microsoft.Graph;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Helpers
{
    public class UserDataLoader
    {
        private readonly GraphServiceClient _client;

        public UserDataLoader(GraphServiceClient client)
        {
            this._client = client;
        }

        public async Task<string> GetUserPhotoBase64(string userId)
        {
            ProfilePhoto photoInfo = null;
            try
            {
                photoInfo = await _client.Users[userId].Photo.Request().GetAsync();
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
            }

            using (var photoMS = await _client.Users[userId].Photo.Content.Request().GetAsync())
            {
                byte[] bytes;
                using (var memoryStream = new MemoryStream())
                {
                    photoMS.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();
                }

                string base64 = Convert.ToBase64String(bytes);
                return $"data:{photoInfo.AdditionalData["@odata.mediaContentType"]};base64,{base64}";
            }

        }
    }
}
