using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fika.Core.Coop.Airdrops.Models
{
	/// <summary>
	/// Created by: SPT team
	/// Link: https://dev.sp-tarkov.com/SPT/Modules/src/branch/master/project/SPT.Custom/Airdrops/Models
	/// </summary>
	public class FikaAirdropLootResultModel
	{
		[JsonProperty("dropType")]
		public string DropType { get; set; }

		[JsonProperty("loot")]
		public IEnumerable<FikaAirdropLootModel> Loot { get; set; }
	}


	public class FikaAirdropLootModel
	{
		[JsonProperty("tpl")]
		public string Tpl { get; set; }

		[JsonProperty("isPreset")]
		public bool IsPreset { get; set; }

		[JsonProperty("stackCount")]
		public int StackCount { get; set; }

		[JsonProperty("id")]
		public string ID { get; set; }
	}
}