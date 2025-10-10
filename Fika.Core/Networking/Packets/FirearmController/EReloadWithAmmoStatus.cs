namespace Fika.Core.Networking.Packets.FirearmController;

/// <summary>
/// Describes the state of a reload operation with ammunition.
/// </summary>
public enum EReloadWithAmmoStatus : byte
{
    /// <summary>
    /// No reload in progress.
    /// </summary>
    None,

    /// <summary>
    /// Reload has started.
    /// </summary>
    StartReload,

    /// <summary>
    /// Reload has completed.
    /// </summary>
    EndReload,

    /// <summary>
    /// Reload has been aborted.
    /// </summary>
    AbortReload
}
