using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TranslatorBot.Services.Media
{
    internal class Translator
    {
        private string _translatorConfigBaseUrl;
        private string _translatorConfigKey;

        public Translator(string translatorConfigBaseUrl, string translatorConfigKey)
        {
            _translatorConfigBaseUrl = translatorConfigBaseUrl;
            _translatorConfigKey = translatorConfigKey;
        }

        public async Task<List<string>> TranslateAsync(string spokenText, string toLanguage)
        {
            throw new NotImplementedException();
        }
    }
}