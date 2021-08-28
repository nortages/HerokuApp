using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TwitchBot.Main
{
    [JsonObject]
    public class BaseCallbackInfo
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)] public bool IsEnabled { get; set; }
        public string Id { get; set; }
        public virtual bool HasCallback { get; set; }
        public Callback Callback { get; set; }
        
        public static void TestFunc(object o, EventArgs e, Tuple<BaseCallbackInfo, Bot> addInfo)
        {
            var (item, bot) = addInfo; 
            item.Callback(o, e, new CallbackArgs {Bot = bot});
        }
        
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (!IsEnabled || !HasCallback) return;

            var methodInfo = typeof(Callbacks)
                .GetMethods()
                .SingleOrDefault(m => CustomAttributeExtensions.GetCustomAttribute<CallbackInfoAttribute>(m)?.Id == Id);

            if (methodInfo == null)
                throw new InvalidOperationException($"Method wasn't found by specified id. Id: {Id}");


            if (methodInfo.ReturnType == typeof(string))
            {
                Callback = (o, e, args) => StringCallbackWrapper(methodInfo, this, o, e, args);
            }
            else if (methodInfo.ReturnType == typeof(void))
            {
                Callback = (o, e, args) => VoidCallbackWrapper(methodInfo, this, o, e, args);
            }
            else if (methodInfo.ReturnType == typeof(Task))
            {
                Callback = (o, e, args) => TaskCallbackWrapper(methodInfo, this, o, e, args);
            }
        }

        public string TaskCallbackWrapper(MethodInfo methodInfo, object obj, params object[] parameters)
        {
            var task = (Task) methodInfo.Invoke(obj, parameters);
            task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            var returnValue = resultProperty.GetValue(task) as string;
            return returnValue;
        }
        
        public string VoidCallbackWrapper(MethodInfo methodInfo, object obj, params object[] parameters)
        {
            methodInfo.Invoke(obj, parameters);
            return null;
        }
        
        public string StringCallbackWrapper(MethodInfo methodInfo, object obj, params object[] parameters)
        {
            return (string)methodInfo.Invoke(obj, parameters);
        }
    }
}