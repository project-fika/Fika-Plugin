// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class HeadlessPacketSender : MonoBehaviour, IPacketSender
    {
        public bool Enabled { get; set; }
        public bool SendState { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }

        private CoopPlayer _player;

        public static HeadlessPacketSender Create(CoopPlayer player)
        {
            HeadlessPacketSender sender = player.gameObject.AddComponent<HeadlessPacketSender>();
            sender._player = player;
            sender.Server = Singleton<FikaServer>.Instance;
            sender.enabled = false;
            sender.Enabled = false;
            sender.SendState = false;
            return sender;
        }

        public void Init()
        {
            enabled = true;
            Enabled = true;
        }

        public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable
        {
            Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public void DestroyThis()
        {
            if (Server != null)
            {
                Server = null;
            }
            if (Client != null)
            {
                Client = null;
            }
            Destroy(this);
        }
    }
}
