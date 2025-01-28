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
            public void HandleRequest(NetPeer peer);
            public void HandleResponse();
            public void Serialize(NetDataWriter writer);
        }

        public enum EGrenadePacketType
        {
            None,
            ExamineWeapon,
            HighThrow,
            LowThrow,
            PullRingForHighThrow,
            PullRingForLowThrow
        };

        public enum EReloadWithAmmoStatus
        {
            None,
            StartReload,
            EndReload,
            AbortReload
        }

        public enum EStationaryCommand
        {
            Occupy,
            Leave,
            Denied
        }

        public enum EProceedType
        {
            EmptyHands,
            FoodClass,
            GrenadeClass,
            MedsClass,
            QuickGrenadeThrow,
            QuickKnifeKick,
            QuickUse,
            UsableItem,
            Weapon,
            Stationary,
            Knife,
            TryProceed
        }

        public enum EFirearmSubPacketType
        {
            ShotInfo,
            ChangeFireMode,
            ToggleAim,
            ExamineWeapon,
            CheckAmmo,
            CheckChamber,
            CheckFireMode,
            ToggleLightStates,
            ToggleScopeStates,
            ToggleLauncher,
            ToggleInventory,
            Loot,
            ReloadMag,
            QuickReloadMag,
            ReloadWithAmmo,
            CylinderMag,
            ReloadLauncher,
            ReloadBarrels,
            Grenade,
            CancelGrenade,
            CompassChange,
            Knife,
            FlareShot,
            RocketShot,
            ReloadBoltAction,
            RollCylinder,
            UnderbarrelSightingRangeUp,
            UnderbarrelSightingRangeDown,
            ToggleBipod,
            LeftStanceChange
        }

        public enum ECommonSubPacketType
        {
            Phrase,
            WorldInteraction,
            ContainerInteraction,
            Proceed,
            HeadLights,
            InventoryChanged,
            Drop,
            Stationary,
            Vault,
            Interaction,
            Mounting,
        }

        public enum EGenericSubPacketType
        {
            ClientExtract,
            ClientConnected,
            ClientDisconnected,
            ExfilCountdown,
            ClearEffects,
            UpdateBackendData,
            SecretExfilFound,
            BorderZone,
            Mine
            //TrainSync,
            //TraderServiceNotification,
        }

        public enum ERequestSubPacketType
        {
            SpawnPoint,
            Weather,
            Exfiltration,
            TraderServices
        }
    }
}
