using BepInEx.Logging;
using Fika.Core.Networking.Http.Models;
using LiteNetLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System;
using System.Net;
using WebSocketSharp;

namespace Fika.Core.Networking.NatPunch
{
    public class FikaNatPunchServer
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.NatPunchServer");

        public string Host { get; set; }
        public string Url { get; set; }
        public string SessionId { get; set; }
        public bool Connected
        {
            get
            {
                return _webSocket.ReadyState == WebSocketState.Open;
            }
        }
        private StunIPEndPoint _stunIpEndPoint;
        public StunIPEndPoint StunIPEndpoint
        {
            get
            {
                return _stunIpEndPoint;
            }
            set
            {
                _stunIpEndPoint = value;
            }
        }

        private WebSocket _webSocket;
        private NetManager _netManager;

        public FikaNatPunchServer(NetManager netManager)
        {
            Host = RequestHandler.Host.Replace("http", "ws");
            SessionId = RequestHandler.SessionId;
            Url = $"{Host}/fika/natpunchrelayservice/{SessionId}?";

            _webSocket = new WebSocket(Url)
            {
                WaitTime = TimeSpan.FromMinutes(1),
                EmitOnPing = true
            };

            _webSocket.OnOpen += WebSocket_OnOpen;
            _webSocket.OnError += WebSocket_OnError;
            _webSocket.OnMessage += WebSocket_OnMessage;
            _webSocket.OnClose += WebSocket_OnClose;

            _netManager = netManager;
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
            logger.LogInfo("Connected to FikaNatPunchRelayService as server");
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            ProcessMessage(e.Data);
        }

        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            logger.LogError($"FikaNatPunchServer Websocket error: {e.Message}");
            _webSocket.Close();
        }

        private void WebSocket_OnClose(object sender, CloseEventArgs e)
        {
            logger.LogInfo($"Disconnected from FikaNatPunchService as server");
        }

        private void ProcessMessage(string data)
        {
            object msgObj = GetRequestObject(data);
            string msgObjType = msgObj.GetType().Name;

            switch (msgObjType)
            {
                case "GetHostStunRequest":
                    GetHostStunRequest getHostStunRequest = (GetHostStunRequest)msgObj;

                    if (_stunIpEndPoint != null)
                    {
                        IPEndPoint clientIpEndPoint = new IPEndPoint(IPAddress.Parse(getHostStunRequest.StunIp), getHostStunRequest.StunPort);

                        NatPunchUtils.PunchNat(_netManager, clientIpEndPoint);

                        SendHostStun(getHostStunRequest.SessionId, _stunIpEndPoint);
                    }
                    break;
            }
        }

        private void Send<T1>(T1 o)
        {
            string data = JsonConvert.SerializeObject(o);
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

            string requestType = obj["requestType"].ToString();

            return requestType switch
            {
                "GetHostStunRequest" => (object)JsonConvert.DeserializeObject<GetHostStunRequest>(data),
                _ => throw new ArgumentException("Invalid requestType received!"),
            };
        }

        public void SendHostStun(string clientId, StunIPEndPoint stunIpEndPoint)
        {
            GetHostStunResponse getHostStunResponse = new GetHostStunResponse(clientId, stunIpEndPoint.Remote.Address.ToString(), stunIpEndPoint.Remote.Port);
            Send(getHostStunResponse);
        }
    }
}
