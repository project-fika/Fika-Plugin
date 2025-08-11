using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Player;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Fika.Core.Networking.Pooling
{
    internal sealed class CommonSubPacketPoolManager : BasePacketPoolManager<ECommonSubPacketType, IPoolSubPacket>
    {
        private static Lazy<CommonSubPacketPoolManager> _instance = new(() => new CommonSubPacketPoolManager(), LazyThreadSafetyMode.None);
        public static CommonSubPacketPoolManager Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public static void Release()
        {
            _instance.Value.ClearPool();
            _instance = null;
        }

        private CommonSubPacketPoolManager()
        {
            _subPacketFactories = new Dictionary<ECommonSubPacketType, Func<IPoolSubPacket>>()
            {
                { ECommonSubPacketType.Phrase, PhrasePacket.CreateInstance },
                { ECommonSubPacketType.WorldInteraction, WorldInteractionPacket.CreateInstance },
                { ECommonSubPacketType.ContainerInteraction, ContainerInteractionPacket.CreateInstance },
                { ECommonSubPacketType.Proceed, ProceedPacket.CreateInstance },
                { ECommonSubPacketType.HeadLights, HeadLightsPacket.CreateInstance },
                { ECommonSubPacketType.InventoryChanged, InventoryChangedPacket.CreateInstance },
                { ECommonSubPacketType.Drop, DropPacket.CreateInstance },
                { ECommonSubPacketType.Stationary, StationaryPacket.CreateInstance },
                { ECommonSubPacketType.Vault, VaultPacket.CreateInstance },
                { ECommonSubPacketType.Interaction, InteractionPacket.CreateInstance },
                { ECommonSubPacketType.Mounting, MountingPacket.CreateInstance },
                { ECommonSubPacketType.Damage, DamagePacket.CreateInstance }
            };
        }
    }
}
