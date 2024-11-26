using Fika.Core.Coop.Players;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets
{
	public class SubPacket
	{
		public interface ISubPacket
		{
			public void Execute(CoopPlayer player);
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
			ExfilCountdown,
			ClearEffects,
			UpdateBackendData
			//TrainSync,
			//TraderServiceNotification,
		}
	}
}
