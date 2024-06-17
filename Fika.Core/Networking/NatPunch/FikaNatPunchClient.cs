using WebSocketSharp;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Fika.Core.Networking.Http.Models;
using SPT.Common.Http;

namespace Fika.Core.Networking.NatPunch
{
    public class FikaNatPunchClient
    {
        public string Host { get; set; }
        public string Url { get; set; }
        public string SessionId { get; set; }
        public bool Connected 
        {
            get
            {
                return _webSocket.ReadyState == WebSocketState.Open ? true : false;
            }
        }

        private WebSocket _webSocket;
        private TaskCompletionSource<string> _receiveTaskCompletion;

        public FikaNatPunchClient()
        {
            Host = $"ws:{RequestHandler.Host.Split(':')[1]}:{FikaPlugin.NatPunchPort.Value}";
            SessionId = RequestHandler.SessionId;
            Url = $"{Host}/{SessionId}?";

            _webSocket = new WebSocket(Url)
            {
                WaitTime = TimeSpan.FromMinutes(1),
                EmitOnPing = true
            };

            _webSocket.OnOpen += WebSocket_OnOpen;
            _webSocket.OnError += WebSocket_OnError;
            _webSocket.OnMessage += WebSocket_OnMessage;
            _webSocket.OnClose += WebSocket_OnClose;
        }

        public void Connect()
        {
            if (_webSocket.ReadyState == WebSocketState.Open)
                return;

            _webSocket.Connect();

            
        }

        public void Close()
        {
            _webSocket.Close();
        }

        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            EFT.UI.ConsoleScreen.Log("Connected to FikaNatPunchService as client");
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            if (_receiveTaskCompletion == null)
                return;

            if (_receiveTaskCompletion.Task.Status == TaskStatus.RanToCompletion)
                return;

            if (e == null)
                return;

            if (string.IsNullOrEmpty(e.Data))
                return;

            _receiveTaskCompletion.SetResult(e.Data);
        }

        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            EFT.UI.ConsoleScreen.LogError($"Websocket error {e}");
            _webSocket.Close();
        }

        private void WebSocket_OnClose(object sender, CloseEventArgs e)
        {
            EFT.UI.ConsoleScreen.Log($"Disconnected from FikaNatPunchService as client");
        }

        private void Send<T1>(T1 o)
        {           
            var data = JsonConvert.SerializeObject(o);
            _webSocket.Send(data);
        }

        private async Task<T2> Receive<T2>()
        {
            var data = await _receiveTaskCompletion.Task;
            var obj = JsonConvert.DeserializeObject<T2>(data);

            return obj;
        }

        private async Task<T2> SendAndReceiveAsync<T1, T2>(T1 o)
        {
            _receiveTaskCompletion = new TaskCompletionSource<string>();

            Send<T1>(o);

            var data = await Receive<T2>();

            return data;
        }

        public async Task<GetHostStunResponse> GetHostStun(GetHostStunRequest getHostStunRequest)
        {
            var result = await SendAndReceiveAsync<GetHostStunRequest, GetHostStunResponse>(getHostStunRequest);

            return result;
        }
    }
}
