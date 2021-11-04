using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Fasterflect;
using TwitchBot.Main;

#nullable disable

namespace TwitchBot.Models
{
    [Table("callback")]
    public partial class CallbackInfo
    {
        [Key] [Column("id")] 
        public string Id { get; set; }

        [Column("is_enabled")] 
        public bool IsEnabled { get; set; }

        // [InverseProperty(nameof(Option.CallbackInfo))]
        public virtual Option ParentOption { get; set; }
    }

    public partial class CallbackInfo
    {
        public object Invoke<T>(object[] callbackParams, CallbackArgs args)
        {
            return typeof(EventHandler).CallMethod(Id, callbackParams.Append(args).ToArray());
        }
    }
}