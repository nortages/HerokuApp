using System;

namespace HerokuApp
{
    public class CallbackArgs<T> where T : EventArgs
    {
        public object sender;
        public T e;
        public Bot bot;
        public bool isTestMode;
        public string lang = "ru";
    }

    public delegate string Callback<T>(CallbackArgs<T> args) where T : EventArgs;
}