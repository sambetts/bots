using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Luis
{
    public partial class LUISConferenceCallRequest 
    {

        public string WhenTimex
            => Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault();

    }
}
