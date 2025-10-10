namespace Fika.Core.Networking.Pooling;

/// <summary>
/// Provides utility methods to create and release all relevant packet pools.
/// </summary>
internal static class PoolUtils
{
    /// <summary>
    /// Creates all packet pools by invoking the <c>CreatePool</c> method on each pool manager instance.
    /// </summary>
    public static void CreateAll()
    {
        FirearmSubPacketPoolManager.Instance.CreatePool();
        CommonSubPacketPoolManager.Instance.CreatePool();
        GenericSubPacketPoolManager.Instance.CreatePool();
    }

    /// <summary>
    /// Releases all packet pools by invoking the <c>Release</c> method on each pool manager.
    /// </summary>
    public static void ReleaseAll()
    {
        FirearmSubPacketPoolManager.Release();
        CommonSubPacketPoolManager.Release();
        GenericSubPacketPoolManager.Release();
    }
}
