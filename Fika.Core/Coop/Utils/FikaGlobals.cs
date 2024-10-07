using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Coop.Players;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Utils
{
	public static class FikaGlobals
	{
		public const string TransitTraderId = "656f0f98d80a697f855d34b1";
		public const string TransiterTraderName = "BTR";

		public static readonly List<EInteraction> BlockedInteractions =
		[
			EInteraction.DropBackpack, EInteraction.NightVisionOffGear, EInteraction.NightVisionOnGear,
			EInteraction.FaceshieldOffGear, EInteraction.FaceshieldOnGear, EInteraction.BipodForwardOn,
			EInteraction.BipodForwardOff, EInteraction.BipodBackwardOn, EInteraction.BipodBackwardOff
		];

		public static float GetOtherPlayerSensitivity()
		{
			return 1f;
		}

		public static float GetLocalPlayerSensitivity()
		{
			return Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseSensitivity;
		}

		public static float GetLocalPlayerAimingSensitivity()
		{
			return Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseAimingSensitivity;
		}

		public static string FormatFileSize(long bytes)
		{
			int unit = 1024;
			if (bytes < unit) { return $"{bytes} B"; }

			int exp = (int)(Math.Log(bytes) / Math.Log(unit));
			return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
		}

		public static void SpawnItemInWorld(Item item, CoopPlayer player)
		{
			StaticManager.BeginCoroutine(SpawnItemRoutine(item, player));
		}

		private static IEnumerator SpawnItemRoutine(Item item, CoopPlayer player)
		{
			List<ResourceKey> collection = [];
			IEnumerable<Item> items = item.GetAllItems();
			foreach (Item subItem in items)
			{
				foreach (ResourceKey resourceKey in subItem.Template.AllResources)
				{
					collection.Add(resourceKey);
				}
			}
			Task loadTask = Singleton<PoolManager>.Instance.LoadBundlesAndCreatePools(PoolManager.PoolsCategory.Raid, PoolManager.AssemblyType.Online,
				[.. collection], JobPriority.Immediate, null, default);

			while (!loadTask.IsCompleted)
			{
				yield return new WaitForEndOfFrame();
			}

			Singleton<GameWorld>.Instance.SetupItem(item, player,
				player.Transform.Original.position + player.Transform.Original.forward + (player.Transform.Original.up / 2), Quaternion.identity);

			if (player.IsYourPlayer)
			{
				ConsoleScreen.Log("Spawned item: " + item.ShortName.Localized());
				yield break;
			}
			ConsoleScreen.Log($"{player.Profile.Info.Nickname} has spawned item: {item.ShortName.Localized()}");
		}
	}
}
