using System.Collections.Generic;

namespace Fika.Core.Coop.Utils
{
	public static class Constants
	{
		public static readonly List<EInteraction> BlockedInteractions =
		[
			EInteraction.DropBackpack, EInteraction.NightVisionOffGear, EInteraction.NightVisionOnGear,
			EInteraction.FaceshieldOffGear, EInteraction.FaceshieldOnGear
		];
	}
}
