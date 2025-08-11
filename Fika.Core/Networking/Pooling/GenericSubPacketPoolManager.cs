using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.World;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Fika.Core.Networking.Pooling
{
    internal sealed class GenericSubPacketPoolManager : BasePacketPoolManager<EGenericSubPacketType, IPoolSubPacket>
    {
        private static Lazy<GenericSubPacketPoolManager> _instance = new(() => new GenericSubPacketPoolManager(), LazyThreadSafetyMode.None);
        public static GenericSubPacketPoolManager Instance
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

        private GenericSubPacketPoolManager()
        {
            _subPacketFactories = new Dictionary<EGenericSubPacketType, Func<IPoolSubPacket>>()
            {
                { EGenericSubPacketType.ClientExtract, ClientExtract.CreateInstance },
                { EGenericSubPacketType.ClientConnected, ClientConnected.CreateInstance },
                { EGenericSubPacketType.ClientDisconnected, ClientDisconnected.CreateInstance },
                { EGenericSubPacketType.ExfilCountdown, ExfilCountdown.CreateInstance },
                { EGenericSubPacketType.ClearEffects, ClearEffects.CreateInstance },
                { EGenericSubPacketType.UpdateBackendData, UpdateBackendData.CreateInstance },
                { EGenericSubPacketType.SecretExfilFound, SecretExfilFound.CreateInstance },
                { EGenericSubPacketType.BorderZone, BorderZoneEvent.CreateInstance },
                { EGenericSubPacketType.Mine, MineEvent.CreateInstance },
                { EGenericSubPacketType.DisarmTripwire, DisarmTripwire.CreateInstance },
                { EGenericSubPacketType.MuffledState, MuffledState.CreateInstance },
                { EGenericSubPacketType.SpawnBTR, BtrSpawn.CreateInstance },
            };
        }
    }
}
