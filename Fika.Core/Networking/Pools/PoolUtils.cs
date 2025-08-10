namespace Fika.Core.Networking.Pools
{
    internal static class PoolUtils
    {
        public static void CreateAll()
        {
            FirearmSubPacketPoolManager.Instance.CreatePool();
            CommonSubPacketPoolManager.Instance.CreatePool();
        }

        public static void ReleaseAll()
        {
            FirearmSubPacketPoolManager.Release();
            CommonSubPacketPoolManager.Release();
        }
    }
}
