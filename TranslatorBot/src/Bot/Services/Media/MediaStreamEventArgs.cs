using Microsoft.Skype.Bots.Media;
using System.Collections.Generic;

namespace TranslatorBot.Services.Media
{
    public class MediaStreamEventArgs
    {
        public List<AudioMediaBuffer> AudioMediaBuffers { get; set; }
    }
}