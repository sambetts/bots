using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslatorBot.Model.Models
{
    public interface ILanguageSettings
    {
        string FromLanguage { get; set; }
        string ToLanguage { get; set; }
    }

    public class LanguageSettings : ILanguageSettings
    {
        public string FromLanguage { get; set; }
        public string ToLanguage { get; set; }
    }
}
