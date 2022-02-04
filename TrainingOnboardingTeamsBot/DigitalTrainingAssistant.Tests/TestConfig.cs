using DigitalTrainingAssistant.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.UnitTests
{
    public class TestConfig : BotConfig
    {
        public TestConfig(IConfiguration configuration) : base(configuration)
        {
            this.TestUserAadObjectId = configuration["TestUserAadObjectId"];
        }

        public string TestUserAadObjectId { get; set; }
    }
}
