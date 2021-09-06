using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TwitchBot.Main;
using TwitchBot.Main.Callbacks;

#nullable disable

namespace TwitchBot.Models
{
    [Table("callback")]
    public partial class CallbackInfo
    {
        public CallbackInfo()
        {
            Options = new HashSet<Option>();
            ServiceCallbacks = new HashSet<ServiceCallback>();
        }

        [Key] [Column("id")]
        public string Id { get; set; }
        [Column("is_enabled")]
        public bool IsEnabled { get; set; }

        [InverseProperty(nameof(Option.CallbackInfo))]
        public virtual ICollection<Option> Options { get; set; }
        [InverseProperty(nameof(ServiceCallback.CallbackInfo))]
        public virtual ICollection<ServiceCallback> ServiceCallbacks { get; set; }
    }

    public partial class CallbackInfo
    {
        public Callback GetCallbackDelegate()
        {
            Callback callback;
            
            var methodInfo = typeof(EventCallbacks)
                .GetMethods()
                .SingleOrDefault(m => CustomAttributeExtensions.GetCustomAttribute<CallbackInfoAttribute>(m)?.Id == Id);

            if (methodInfo == null)
                throw new InvalidOperationException($"Method wasn't found by specified id. Id: {Id}");
            
            if (methodInfo.ReturnType == typeof(string))
            {
                callback = (o, e, args) => StringCallbackWrapper(methodInfo, this, o, e, args);
            }
            else if (methodInfo.ReturnType == typeof(void))
            {
                callback = (o, e, args) => VoidCallbackWrapper(methodInfo, this, o, e, args);
            }
            else if (methodInfo.ReturnType == typeof(Task))
            {
                callback = (o, e, args) => TaskCallbackWrapper(methodInfo, this, o, e, args);
            }
            else
            {
                throw new InvalidOperationException("Invalid return type");
            }
            
            return callback;
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
