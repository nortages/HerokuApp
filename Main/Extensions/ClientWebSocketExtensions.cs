using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HerokuApp.Main.Extensions
{
    public static class ClientWebSocketExtensions
    {
        public static async Task SendAsync(this ClientWebSocket socket, string data)
        {
            var encoded = Encoding.UTF8.GetBytes(data);
            var arraySegment = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static async Task<string> ReceiveAsync(this ClientWebSocket socket)
        {
            var cancelToken = CancellationToken.None;
            var response = "";
            WebSocketReceiveResult received;

            do
            {
                var buffer = new byte[1024];
                received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
                if (received.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }
                string text = Encoding.UTF8.GetString(buffer, 0, received.Count);
                response += text;
            }
            while (!received.EndOfMessage);

            return response;
        }
    }
}
