using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Utils
{
	public enum EMatchmakerType
	{
		Single = 0,
		GroupPlayer = 1,
		GroupLeader = 2
	}

	public static class FikaBackendUtils
	{
		internal static MatchMakerAcceptScreen MatchMakerAcceptScreenInstance;
		/// <summary>
		/// The local player <see cref="EFT.Profile"/>
		/// </summary>
		public static Profile Profile { get; internal set; }
		/// <summary>
		/// The name of the local player PMC
		/// </summary>
		public static string PMCName { get; internal set; }
		public static bool IsScav { get; internal set; }
		public static EMatchmakerType MatchingType { get; internal set; } = EMatchmakerType.Single;
		public static bool IsDedicated = false;
		public static bool IsReconnect { get; internal set; }
		public static bool IsDedicatedGame { get; set; } = false;
		public static bool IsDedicatedRequester { get; set; }
		public static bool IsTransit { get; internal set; }
		public static bool IsSpectator { get; internal set; }
		public static bool IsHostNatPunch { get; internal set; }
		public static int HostExpectedNumberOfPlayers { get; set; } = 1;
		public static string RemoteIp { get; internal set; }
		public static int RemotePort { get; internal set; }
		public static int LocalPort { get; internal set; } = 0;
		public static string HostLocationId { get; internal set; }
		internal static bool RequestFikaWorld = false;
		internal static Vector3 ReconnectPosition = Vector3.zero;
		internal static RaidSettings CachedRaidSettings;
		internal static PlayersRaidReadyPanel PlayersRaidReadyPanel;
		internal static MatchMakerGroupPreview MatchMakerGroupPreview;

		public static bool IsServer
		{
			get
			{
				return MatchingType == EMatchmakerType.GroupLeader;
			}
		}
		public static bool IsClient
		{
			get
			{
				return MatchingType == EMatchmakerType.GroupPlayer;
			}
		}
		public static bool IsSinglePlayer
		{
			get
			{
				return Singleton<FikaServer>.Instantiated
					&& Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0;
			}
		}
		public static string GroupId { get; internal set; }
		public static string RaidCode { get; internal set; }
		public static GClass1348 TransitData
		{
			get
			{
				if (transitData == null)
				{
					return new()
					{
						isLocationTransition = true,
						transitionCount = 0,
						transitionRaidId = FikaGlobals.DefaultTransitId,
						visitedLocations = []
					};
				}

				return transitData;
			}
			internal set
			{
				transitData = value;
			}
		}

		private static GClass1348 transitData;

		internal static void ResetTransitData()
		{
			TransitData = null;
		}

		public static bool JoinMatch(string profileId, string serverId, out CreateMatch result, out string errorMessage)
		{
			result = new CreateMatch();
			errorMessage = $"No server matches the data provided or the server no longer exists";

			if (MatchMakerAcceptScreenInstance == null)
			{
				return false;
			}

			MatchJoinRequest body = new(serverId, profileId);
			result = FikaRequestHandler.RaidJoin(body);

			if (result.GameVersion != FikaPlugin.EFTVersionMajor)
			{
				errorMessage = $"You are attempting to use a different version of EFT than what the server is running.\nClient: {FikaPlugin.EFTVersionMajor}\nServer: {result.GameVersion}";
				return false;
			}

			Version detectedFikaVersion = Assembly.GetExecutingAssembly().GetName().Version;
			if (result.FikaVersion != detectedFikaVersion)
			{
				errorMessage = $"You are attempting to use a different version of Fika than what the server is running.\nClient: {detectedFikaVersion}\nServer: {result.FikaVersion}";
				return false;
			}

			RaidCode = result.RaidCode;

			return true;
		}

		public static async Task CreateMatch(string profileId, string hostUsername, RaidSettings raidSettings)
		{
			NotificationManagerClass.DisplayWarningNotification(LocaleUtils.STARTING_RAID.Localized());
			long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
			string raidCode = GenerateRaidCode(6);
			CreateMatch body = new(raidCode, profileId, hostUsername, FikaBackendUtils.IsSpectator, timestamp, raidSettings,
				HostExpectedNumberOfPlayers, raidSettings.Side, raidSettings.SelectedDateTime);

			await FikaRequestHandler.RaidCreate(body);

			GroupId = profileId;
			MatchingType = EMatchmakerType.GroupLeader;

			RaidCode = raidCode;
		}

		internal static string GenerateRaidCode(int length)
		{
			System.Random random = new();
			char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
			string raidCode = "";
			for (int i = 0; i < length; i++)
			{
				int charIndex = random.Next(chars.Length);
				raidCode += chars[charIndex];
			}

			return raidCode;
		}

		internal static void AddPartyMembers(Dictionary<Profile, bool> profiles)
		{
			if (IsDedicated)
			{
				return;
			}

			if (Profile == null)
			{
				FikaPlugin.Instance.FikaLogger.LogError("AddPartyMembers: Own profile was null!");
				return;
			}

			GClass3771<GClass1323> playerList = [];
			foreach (KeyValuePair<Profile, bool> kvp in profiles)
			{
				Profile profile = kvp.Key;
				InfoClass info = profile.Info;
				GClass1322 infoSet = new()
				{
					AccountId = profile.AccountId,
					Id = profile.Id,
					IsLeader = kvp.Value,
					Info = new()
					{
						Level = info.Level,
						MemberCategory = info.MemberCategory,
						SelectedMemberCategory = info.SelectedMemberCategory,
						Nickname = profile.GetCorrectedNickname(),
						Side = info.Side,
						GameVersion = info.GameVersion,
						HasCoopExtension = info.HasCoopExtension
					}
				};
				GClass1323 visualProfile = new(infoSet)
				{
					PlayerVisualRepresentation = profile.GetVisualEquipmentState(false)
				};

				playerList.Add(visualProfile);
			}

			if (TarkovApplication.Exist(out TarkovApplication app))
			{
				MatchmakerPlayerControllerClass controller = app.MatchmakerPlayerControllerClass;
				if (controller != null)
				{
					//controller.GroupPlayers.Add(visualProfile);
					MenuUI menuUi = Singleton<MenuUI>.Instance;
					if (menuUi != null)
					{
						PartyInfoPanel panel = Traverse.Create(menuUi.MatchmakerTimeHasCome).Field<PartyInfoPanel>("_partyInfoPanel").Value;
						//PartyPlayerItem playerItem = Traverse.Create(panel).Field<PartyPlayerItem>("_playerItemTemplate").Value;
						panel.Close();
						panel.Show(playerList, Profile, false);
						return;
					}
					FikaPlugin.Instance.FikaLogger.LogWarning("AddPartyMembers: MenuUI was null!");
					return;
				}
				FikaPlugin.Instance.FikaLogger.LogWarning("AddPartyMembers: MatchmakerPlayerControllerClass was null!");
			}
			FikaPlugin.Instance.FikaLogger.LogWarning("AddPartyMembers: TarkovApplication was null!");
		}
	}
}
