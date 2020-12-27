using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using HerokuApp;

namespace HerokuApp.Controllers
{
    [ApiController]
    [Route("stream")]
    public class TwitchStreamController : ControllerBase
    {
        [HttpGet]
        [Route("channel-info")]
        public void GetChannelInfo()
        {
            var channelInfo = new Dictionary<string, string>
            {
                { "ChannelName", Config.ChannelName },
                { "ChannelId", Config.ChannelUserID }
            };
            var jsonString = JsonConvert.SerializeObject(channelInfo);

            var context = ControllerContext.HttpContext;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.Headers.Add("Access-Control-Allow-Origin", "chrome-extension://*");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Access-Control-Allow-Origin");

            var method = context.Request.Method;
            if (method == "OPTIONS") return;

            context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(jsonString)));
        }

        [HttpGet]
        [Route("last-messages/{num}")]
        public void GetLastMessages(int num)
        {
            var context = ControllerContext.HttpContext;
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            // Create the response

            context.Response.Headers.Add("Access-Control-Allow-Origin", "https://www.twitch.tv");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Access-Control-Allow-Origin");

            var method = context.Request.Method;
            if (method == "OPTIONS") return;

            var jsonString = JsonConvert.SerializeObject(TwitchChatBot.chatMessages.TakeLast(num));
            context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(jsonString)));
        }
    }
}
