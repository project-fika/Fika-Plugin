using Comfort.Common;
using System;
using System.Collections.Generic;

namespace Fika.Core.Coop.Utils
{
	public static class FikaGlobals
	{
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
	}
}
