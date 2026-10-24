using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fika.Core.Networking.Websocket;

public sealed class FikaWebSocket(string url, string sessionId, TimeSpan keepAlive)
{
    private ClientWebSocket _socket;
    private bool _closing;

    public event Action Opened;
    public event Action<string> MessageReceived;
    public event Action<string> Errored;
    public event Action Closed;

    public bool IsOpen
    {
        get
        {
            return _socket?.State == WebSocketState.Open;
        }
    }

    public bool IsConnecting { get; private set; }

    public void Connect()
    {
        _ = ConnectAsync();
    }

    public void Close()
    {
        _closing = true;
        var socket = _socket;
        if (socket?.State == WebSocketState.Open)
        {
            _ = socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }
    }

    private async Task ConnectAsync()
    {
        if (IsOpen || IsConnecting)
        {
            return;
        }

        IsConnecting = true;
        _closing = false;
        _socket?.Dispose();
        ClientWebSocket socket = new();
        socket.Options.KeepAliveInterval = keepAlive;
        socket.Options.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(sessionId + ":")));
        socket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        _socket = socket;

        try
        {
            await socket.ConnectAsync(new Uri(url), CancellationToken.None);
        }
        catch (Exception ex)
        {
            IsConnecting = false;
            Errored?.Invoke(ex.Message);
            Closed?.Invoke();
            return;
        }

        IsConnecting = false;
        Opened?.Invoke();
        _ = ReceiveAsync(socket);
    }

    private async Task ReceiveAsync(ClientWebSocket socket)
    {
        var buffer = new byte[8192];
        using MemoryStream message = new();
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    MessageReceived?.Invoke(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
                }

                message.SetLength(0);
            }
        }
        catch (Exception ex)
        {
            if (!_closing)
            {
                Errored?.Invoke(ex.Message);
            }
        }

        if (!_closing)
        {
            Closed?.Invoke();
        }
    }
}
