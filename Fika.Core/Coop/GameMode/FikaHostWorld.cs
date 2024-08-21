using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using LiteNetLib;

namespace Fika.Core.Coop.GameMode
{
	/// <summary>
	/// Currently used to keep track of interactable objects, in the future this will be used to sync reconnects
	/// </summary>
	public class FikaHostWorld : World
	{
		private FikaServer server;
		private GameWorld gameWorld;

		protected void Start()
		{
			server = Singleton<FikaServer>.Instance;
			gameWorld = GetComponent<GameWorld>();
		}

		protected void FixedUpdate()
		{
			int grenadesCount = gameWorld.Grenades.Count;
			if (grenadesCount > 0)
			{
				for (int i = 0; i < grenadesCount; i++)
				{
					Throwable throwable = gameWorld.Grenades.GetByIndex(i);
					gameWorld.method_2(throwable);
				}
			}

			int grenadePacketsCount = gameWorld.GrenadesCriticalStates.Count;
			if (grenadePacketsCount > 0)
			{
				for (int i = 0; i < grenadePacketsCount; i++)
				{
					ThrowablePacket packet = new()
					{
						Data = gameWorld.GrenadesCriticalStates[i]
					};

					server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
				}
			}

			gameWorld.GrenadesCriticalStates.Clear();
		}

		/// <summary>
		/// Sets up all the <see cref="BorderZone"/>s on the map
		/// </summary>
		public override void SubscribeToBorderZones(BorderZone[] zones)
		{
			foreach (BorderZone borderZone in zones)
			{
				borderZone.PlayerShotEvent += OnBorderZoneShot;
			}
		}

		/// <summary>
		/// Triggered when a <see cref="BorderZone"/> triggers (only runs on host)
		/// </summary>
		/// <param name="player"></param>
		/// <param name="zone"></param>
		/// <param name="arg3"></param>
		/// <param name="arg4"></param>
		private void OnBorderZoneShot(IPlayerOwner player, BorderZone zone, float arg3, bool arg4)
		{
			BorderZonePacket packet = new()
			{
				ProfileId = player.iPlayer.ProfileId,
				ZoneId = zone.Id
			};

			Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}
	}
}
