using System;
using System.Threading;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking.Pools
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
            _subPacketFactories = [];
        }
    }
}
