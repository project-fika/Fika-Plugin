using LiteNetLib;
using System;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SPT.Common.Http;
using WebSocketSharp;
using Fika.Core.Networking.Http.Models;

namespace Fika.Core.Networking.NatPunch
{
    public class FikaNatPunchServer
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
        public StunIpEndPoint StunIpEndPoint { get; set; }

        private WebSocket _webSocket;
        private NetManager _netManager;

        public FikaNatPunchServer(NetManager netManager, StunIpEndPoint stunIpEndPoint)
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

            _netManager = netManager;

            StunIpEndPoint = stunIpEndPoint;
        }

        public void Connect()
        {
            _webSocket.Connect();
        }

        public void Close()
        {
            _webSocket.Close();
        }

        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            EFT.UI.ConsoleScreen.Log("Connected to FikaNatPunchService as server");
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            if (e == null)
                return;

            if (string.IsNullOrEmpty(e.Data))
                return;

            ProcessMessage(e.Data);
        }

        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            EFT.UI.ConsoleScreen.LogError($"Websocket error: {e}");
            _webSocket.Close();
        }

        private void ProcessMessage(string data)
        {
            var msgObj = GetRequestObject(data);
            var msgObjType = msgObj.GetType().Name;

            switch (msgObjType)
            {
                case "GetHostStunRequest":
                    var getHostStunRequest = (GetHostStunRequest)msgObj;

                    if (StunIpEndPoint != null)
                    {
                        IPEndPoint clientIpEndPoint = new IPEndPoint(IPAddress.Parse(getHostStunRequest.StunIp), getHostStunRequest.StunPort);

                        NatPunchUtils.PunchNat(_netManager, clientIpEndPoint);

                        SendHostStun(getHostStunRequest.SessionId, StunIpEndPoint);
                    }
                    break;
            }
        }

        private void Send<T1>(T1 o)
        {
            var data = JsonConvert.SerializeObject(o);
            _webSocket.Send(data);
        }

        private object GetRequestObject(string data)
        {
            // We're doing this literally once, so this is fine. Might need to
            // refactor to use StreamReader to detect request type later.
            JObject obj = JObject.Parse(data);

            if (!obj.ContainsKey("requestType"))
            {
                throw new NullReferenceException("requestType");
            }

            var requestType = obj["requestType"].ToString();

            switch (requestType)
            {
                case "GetHostStunRequest":
                    return JsonConvert.DeserializeObject<GetHostStunRequest>(data);
                default:
                    throw new ArgumentException("Invalid requestType received!");
            }
        }

        public void SendHostStun(string clientId, StunIpEndPoint stunIpEndPoint)
        {
            var getHostStunResponse = new GetHostStunResponse(clientId, stunIpEndPoint.Remote.Address.ToString(), stunIpEndPoint.Remote.Port);
            Send(getHostStunResponse);
        }
    }
}
