﻿using System;

namespace DigitalTrainingAssistant.Models
{
    public abstract class BotException : Exception
    {
        public BotException() { }
        public BotException(string message) : base(message)
        {
        }
    }

    public class BotSharePointAccessException : BotException
    {
    }

    public class GraphAccessException : BotException
    {
        public GraphAccessException(string msg) : base(msg) { }
    }
}