using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Cards
{
    public abstract class BaseAdaptiveCard
    {

        public abstract string GetCardContent();

        internal string ReplaceVal(string json, string fieldName, string val)
        {
            json = json.Replace(fieldName, val);

            return json;
        }

        protected string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(DigitalTrainingAssistant.Bot)))
            {
                var manifests = assembly.GetManifestResourceNames();
                if (manifests.Any(str => str.EndsWith(name)))
                {
                    resourcePath = manifests.Single(str => str.EndsWith(name));
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(name));
                }
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        public Attachment GetCard()
        {
            dynamic cardJson = JsonConvert.DeserializeObject(this.GetCardContent());

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = cardJson,
            };
        }
    }
}
