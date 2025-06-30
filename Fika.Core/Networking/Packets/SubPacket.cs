using Fika.Core.Coop.Players;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public class SubPacket
    {
        public interface ISubPacket
        {
            public void Execute(CoopPlayer player = null);
            public void Serialize(NetDataWriter writer);
        }

        public interface IRequestPacket
        {
            public void HandleRequest(NetPeer peer, FikaServer server);
            public void HandleResponse();
            public void Serialize(NetDataWriter writer);
        }

        /// <summary>
        /// Represents types of grenade packet actions.
        /// </summary>
        public enum EGrenadePacketType : byte
        {
            /// <summary>
            /// No grenade action.
            /// </summary>
            None,

            /// <summary>
            /// Examine the weapon while holding a grenade.
            /// </summary>
            ExamineWeapon,

            /// <summary>
            /// Perform a high grenade throw.
            /// </summary>
            HighThrow,

            /// <summary>
            /// Perform a low grenade throw.
            /// </summary>
            LowThrow,

            /// <summary>
            /// Pull the ring in preparation for a high throw.
            /// </summary>
            PullRingForHighThrow,

            /// <summary>
            /// Pull the ring in preparation for a low throw.
            /// </summary>
            PullRingForLowThrow
        }

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

        /// <summary>
        /// Represents commands related to stationary equipment or positions.
        /// </summary>
        public enum EStationaryCommand : byte
        {
            /// <summary>
            /// Occupy the stationary turret.
            /// </summary>
            Occupy,

            /// <summary>
            /// Leave the stationary turret.
            /// </summary>
            Leave,

            /// <summary>
            /// Access to the turret was denied.
            /// </summary>
            Denied
        }

        /// <summary>
        /// Represents the type of context-sensitive interaction (proceed action).
        /// </summary>
        public enum EProceedType : byte
        {
            /// <summary>
            /// No item held (empty hands).
            /// </summary>
            EmptyHands,

            /// <summary>
            /// Food class item.
            /// </summary>
            FoodClass,

            /// <summary>
            /// Grenade class item.
            /// </summary>
            GrenadeClass,

            /// <summary>
            /// Meds class item.
            /// </summary>
            MedsClass,

            /// <summary>
            /// Quick grenade throw action.
            /// </summary>
            QuickGrenadeThrow,

            /// <summary>
            /// Quick knife kick action.
            /// </summary>
            QuickKnifeKick,

            /// <summary>
            /// Quick use action of an item.
            /// </summary>
            QuickUse,

            /// <summary>
            /// Usable item interaction.
            /// </summary>
            UsableItem,

            /// <summary>
            /// Weapon held.
            /// </summary>
            Weapon,

            /// <summary>
            /// Stationary held (turret).
            /// </summary>
            Stationary,

            /// <summary>
            /// Knife held.
            /// </summary>
            Knife,

            /// <summary>
            /// Attempt to proceed action.
            /// </summary>
            TryProceed
        }

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

        /// <summary>
        /// Represents common sub-packet types for general player actions.
        /// </summary>
        public enum ECommonSubPacketType : byte
        {
            /// <summary>
            /// Player phrase.
            /// </summary>
            Phrase,

            /// <summary>
            /// Interaction with the world environment.
            /// </summary>
            WorldInteraction,

            /// <summary>
            /// Interaction with containers.
            /// </summary>
            ContainerInteraction,

            /// <summary>
            /// Proceed action.
            /// </summary>
            Proceed,

            /// <summary>
            /// Headlights toggled or updated.
            /// </summary>
            HeadLights,

            /// <summary>
            /// Inventory contents changed.
            /// </summary>
            InventoryChanged,

            /// <summary>
            /// Item dropped.
            /// </summary>
            Drop,

            /// <summary>
            /// Stationary interaction.
            /// </summary>
            Stationary,

            /// <summary>
            /// Vault or climbing action.
            /// </summary>
            Vault,

            /// <summary>
            /// Generic interaction event.
            /// </summary>
            Interaction,

            /// <summary>
            /// Mounting or dismounting action.
            /// </summary>
            Mounting
        }

        /// <summary>
        /// Represents generic game-level packet events unrelated to specific systems.
        /// </summary>
        public enum EGenericSubPacketType : byte
        {
            /// <summary>
            /// Client extraction event.
            /// </summary>
            ClientExtract,

            /// <summary>
            /// Client connected to the server.
            /// </summary>
            ClientConnected,

            /// <summary>
            /// Client disconnected from the server.
            /// </summary>
            ClientDisconnected,

            /// <summary>
            /// Exfiltration countdown event.
            /// </summary>
            ExfilCountdown,

            /// <summary>
            /// Clear all active effects.
            /// </summary>
            ClearEffects,

            /// <summary>
            /// Update backend data.
            /// </summary>
            UpdateBackendData,

            /// <summary>
            /// Secret exfiltration found event.
            /// </summary>
            SecretExfilFound,

            /// <summary>
            /// Border zone event.
            /// </summary>
            BorderZone,

            /// <summary>
            /// Mine triggered.
            /// </summary>
            Mine,

            /// <summary>
            /// Disarm a tripwire event.
            /// </summary>
            DisarmTripwire,

            /// <summary>
            /// Player muffled state changed.
            /// </summary>
            MuffledState,

            /// <summary>
            /// Spawns the BTR vehicle.
            /// </summary>
            SpawnBTR

            /// <summary>
            /// Train synchronization event (commented out).
            /// </summary>
            // TrainSync,

            /// <summary>
            /// Trader service notification event (commented out).
            /// </summary>
            // TraderServiceNotification,
        }

        /// <summary>
        /// Represents packet types used for requesting environmental or server state data.
        /// </summary>
        public enum ERequestSubPacketType : byte
        {
            /// <summary>
            /// Spawn point request.
            /// </summary>
            SpawnPoint,

            /// <summary>
            /// Weather update request.
            /// </summary>
            Weather,

            /// <summary>
            /// Exfiltration request.
            /// </summary>
            Exfiltration,

            /// <summary>
            /// Request trader services information.
            /// </summary>
            TraderServices,

            /// <summary>
            /// Request character synchronization.
            /// </summary>
            CharacterSync
        }

    }
}
