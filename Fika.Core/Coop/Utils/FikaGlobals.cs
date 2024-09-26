using Comfort.Common;
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
	}
}
