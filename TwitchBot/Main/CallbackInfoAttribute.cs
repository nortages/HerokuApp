using System;

namespace TwitchBot.Main
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CallbackInfoAttribute : Attribute
    {
        public string Id { get; set; }
    }
}