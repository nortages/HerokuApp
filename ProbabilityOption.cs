using System;
using System.Collections.Generic;
using TwitchLib.Client.Events;

namespace HerokuApp
{
    public class Option
    {
        public Func<Option, OnChatCommandReceivedArgs, string> callback;
        public Dictionary<string, int> usageFrequency = new Dictionary<string, int>();
        public List<ProbabilityOption> commandOptions = new List<ProbabilityOption>();
        public List<double> convertedProbabilities;
    }

    public class ProbabilityOption : Option
    {
        public string title;
        public Command command;
        public double probability;
    }
}
