using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TwitchBot.Controllers
{
    [ApiController]
    [Route("twitch-webhooks")]
    public class TwitchWebhookController : ControllerBase
    {
        private readonly ILogger<TwitchWebhookController> _logger;

        public TwitchWebhookController(ILogger<TwitchWebhookController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("streams")]
        public void VerifySubscriptionToTopic()
        {
            var context = ControllerContext.HttpContext;
            var query = context.Request.Query;
            var hubChallenge = query["hub.challenge"];
            context.Response.StatusCode = (int) HttpStatusCode.OK;
            context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(hubChallenge)));
        }

        [HttpPost]
        [Route("streams")]
        public void NewEventOccured()
        {
            var context = ControllerContext.HttpContext;
            Console.WriteLine("\nA new stream event occured!");

            var req = context.Request;
            req.EnableBuffering();
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(req.Body, leaveOpen: true);
            var bodyStr = reader.ReadToEnd();
            Console.WriteLine(bodyStr);
        }

        [HttpGet]
        [Route("test")]
        public void TestHiMark()
        {
            var context = ControllerContext.HttpContext;
            context.Response.StatusCode = (int) HttpStatusCode.OK;

            // Create the response
            var method = context.Request.Method;
            if (method == "OPTIONS") Console.WriteLine("\nGot an OPTIONS request!");

            context.Response.Headers.Add("Access-Control-Allow-Origin", "https://www.twitch.tv");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Access-Control-Allow-Origin");
            var responseText = "Oh, hi Mark!";
            context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(responseText)));
        }
    }
}