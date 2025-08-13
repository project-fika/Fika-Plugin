namespace Fika.Core.Networking.Packets;

/// <summary>
/// Describes sub-packet types related to firearms and combat actions.
/// </summary>
public enum EFirearmSubPacketType : byte
{
    /// <summary>
    /// Information about a shot fired.
    /// </summary>
    ShotInfo,

    /// <summary>
    /// Change the firearm's fire mode.
    /// </summary>
    ChangeFireMode,

    /// <summary>
    /// Toggle aiming state.
    /// </summary>
    ToggleAim,

    /// <summary>
    /// Examine the weapon.
    /// </summary>
    ExamineWeapon,

    /// <summary>
    /// Check the ammo count.
    /// </summary>
    CheckAmmo,

    /// <summary>
    /// Check the chamber status.
    /// </summary>
    CheckChamber,

    /// <summary>
    /// Check the current fire mode.
    /// </summary>
    CheckFireMode,

    /// <summary>
    /// Toggle the light states on the firearm.
    /// </summary>
    ToggleLightStates,

    /// <summary>
    /// Toggle the scope states on the firearm.
    /// </summary>
    ToggleScopeStates,

    /// <summary>
    /// Toggle the launcher attachment.
    /// </summary>
    ToggleLauncher,

    /// <summary>
    /// Toggle the inventory view or state.
    /// </summary>
    ToggleInventory,

    /// <summary>
    /// Perform a loot action.
    /// </summary>
    Loot,

    /// <summary>
    /// Reload the magazine.
    /// </summary>
    ReloadMag,

    /// <summary>
    /// Perform a quick reload of the magazine.
    /// </summary>
    QuickReloadMag,

    /// <summary>
    /// Reload using ammo.
    /// </summary>
    ReloadWithAmmo,

    /// <summary>
    /// Reload the cylinder magazine.
    /// </summary>
    CylinderMag,

    /// <summary>
    /// Reload the launcher.
    /// </summary>
    ReloadLauncher,

    /// <summary>
    /// Reload the barrels.
    /// </summary>
    ReloadBarrels,

    /// <summary>
    /// Grenade-related action.
    /// </summary>
    Grenade,

    /// <summary>
    /// Cancel a grenade action.
    /// </summary>
    CancelGrenade,

    /// <summary>
    /// Toggle the compass.
    /// </summary>
    CompassChange,

    /// <summary>
    /// Knife-related action.
    /// </summary>
    Knife,

    /// <summary>
    /// Flare fired.
    /// </summary>
    FlareShot,

    /// <summary>
    /// Rocket shot fired.
    /// </summary>
    RocketShot,

    /// <summary>
    /// Reload bolt-action firearm.
    /// </summary>
    ReloadBoltAction,

    /// <summary>
    /// Roll the cylinder magazine.
    /// </summary>
    RollCylinder,

    /// <summary>
    /// Increase underbarrel sighting range.
    /// </summary>
    UnderbarrelSightingRangeUp,

    /// <summary>
    /// Decrease underbarrel sighting range.
    /// </summary>
    UnderbarrelSightingRangeDown,

    /// <summary>
    /// Toggle bipod deployment.
    /// </summary>
    ToggleBipod,

    /// <summary>
    /// Left stance changed.
    /// </summary>
    LeftStanceChange
}
