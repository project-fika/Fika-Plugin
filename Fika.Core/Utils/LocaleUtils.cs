using System;
using System.Collections.Generic;

namespace Fika.Core.Utils
{
	public static class LocaleUtils
	{
		private static readonly List<char> vowels = ['A', 'E', 'I', 'O', 'U'];

		/// <summary>
		/// Used to get the prefix of a word for EN locale, no longer used
		/// </summary>
		/// <param name="word"></param>
		/// <returns></returns>
		[Obsolete("No longer used")]
		public static string GetPrefix(string word)
		{
			char firstLetter = char.ToUpper(word[0]);

			if (vowels.Contains(firstLetter))
			{
				return "an";
			}

			return "a";
		}

		public const string RECEIVED_SHARED_QUEST_PROGRESS = "F_Client_ReceivedSharedQuestProgress";
		public const string RECEIVED_SHARED_ITEM_PICKUP = "F_Client_ReceivedSharedItemPickup";
		public const string RECEIVED_SHARED_ITEM_PLANT = "F_Client_ReceivedSharedItemPlant";
		public const string RECEIVE_PING = "F_Client_ReceivePing";
		public const string RECEIVE_PING_OBJECT = "F_Client_ReceivePingObject";
		public const string FREECAM_INPUT_DISABLED = "F_Client_FreeCamInputDisabled";
		public const string RANDOM_SPAWNPOINTS = "F_Client_RandomSpawnPoints";
		public const string METABOLISM_DISABLED = "F_Client_MetabolismDisabled";
		public const string PLAYER_MIA = "F_Client_YouAreMIA";
		public const string KILLED_BOSS = "F_Client_KilledBoss";
		public const string GROUP_MEMBER_SPAWNED = "F_Client_GroupMemberSpawned";
		public const string GROUP_MEMBER_EXTRACTED = "F_Client_GroupMemberExtracted";
		public const string GROUP_MEMBER_DIED_FROM = "F_Client_GroupMemberDiedFrom";
		public const string GROUP_MEMBER_DIED = "F_Client_GroupMemberDied";
		public const string CONNECTED_TO_SERVER = "F_Client_ConnectedToServer";
		public const string SERVER_STARTED = "F_Client_ServerStarted";
		public const string NO_VALID_IP = "F_Client_CouldNotFindValidIP";
		public const string RECONNECT_REQUESTED = "F_Client_ReconnectRequested";
		public const string PEER_CONNECTED = "F_Client_PeerConnected";
		public const string PEER_DISCONNECTED = "F_Client_PeerDisconnected";
		public const string CONNECTING_TO_SESSION = "F_Client_ConnectingToSession";
		public const string ITEM_BLACKLISTED = "F_Client_ItemIsBlacklisted";
		public const string ITEM_CONTAINS_BLACKLISTED = "F_Client_ItemsContainsBlacklisted";
		public const string SAVED_PROFILE = "F_Client_SavedProfile";
		public const string UNKNOWN_ERROR = "F_Client_UnknownError";
		public const string HOST_CANNOT_EXTRACT = "F_Client_HostCannotExtract";
		public const string HOST_CANNOT_EXTRACT_MENU = "F_Client_HostCannotExtractMenu";
		public const string HOST_WAIT_5_SECONDS = "F_Client_Wait5Seconds";
		public const string STARTING_RAID = "F_Client_StartingRaid";
		public const string LOST_CONNECTION = "F_Client_LostConnection";
		public const string STARTING_RAID_ON_DEDICATED = "F_Client_StartingRaidOnDedicated";
		public const string UI_SENDITEM_HEADER = "F_SendItem_Header";
		public const string UI_SENDITEM_BUTTON = "F_SendItem_SendButton";
		public const string UI_MM_RAIDSHEADER = "F_MMUI_RaidsHeader";
		public const string UI_MM_HOST_BUTTON = "F_MMUI_HostRaidButton";
		public const string UI_MM_JOIN_BUTTON = "F_MMUI_JoinButton";
		public const string UI_MM_SELECT_AMOUNT_HEADER = "F_MMUI_SelectAmountHeader";
		public const string UI_MM_SELECT_AMOUNT_DESCRIPTION = "F_MMUI_SelectAmountDescription";
		public const string UI_MM_USE_DEDICATED_HOST = "F_MMUI_UseDedicatedHost";
		public const string UI_MM_START_BUTTON = "F_MMUI_StartButton";
		public const string UI_MM_LOADING_HEADER = "F_MMUI_LoadingScreenHeader";
		public const string UI_MM_LOADING_DESCRIPTION = "F_MMUI_LoadingScreenDescription";
		public const string UI_CANNOT_JOIN_RAID_OTHER_MAP = "F_UI_CannotJoinRaidOtherMap";
		public const string UI_CANNOT_JOIN_RAID_OTHER_TIME = "F_UI_CannotJoinRaidOtherTime";
		public const string UI_CANNOT_JOIN_RAID_SCAV_AS_PMC = "F_UI_CannotJoinRaidScavAsPMC";
		public const string UI_CANNOT_JOIN_RAID_PMC_AS_SCAV = "F_UI_CannotJoinRaidPMCAsScav";
		public const string UI_HOST_STILL_LOADING = "F_UI_HostStillLoading";
		public const string UI_RAID_IN_PROGRESS = "F_UI_RaidInProgress";
		public const string UI_REJOIN_RAID = "F_UI_ReJoinRaid";
		public const string UI_CANNOT_REJOIN_RAID_DIED = "F_UI_CannotReJoinRaidDied";
		public const string UI_JOIN_RAID = "F_UI_JoinRaid";
		public const string UI_ERROR_CONNECTING = "F_UI_ErrorConnecting";
		public const string UI_UNABLE_TO_CONNECT = "F_UI_UnableToConnect";
		public const string UI_PINGER_START_FAIL = "F_UI_FikaPingerFailStart";
		public const string UI_REFRESH_RAIDS = "F_UI_RefreshRaids";
		public const string UI_DEDICATED_ERROR = "F_UI_DedicatedError";
		public const string UI_ERROR_FORCE_IP_HEADER = "F_UI_ErrorForceIPHeader";
		public const string UI_ERROR_FORCE_IP = "F_UI_ErrorForceIP";
		public const string UI_ERROR_BIND_IP_HEADER = "F_UI_ErrorBindIPHeader";
		public const string UI_ERROR_BIND_IP = "F_UI_ErrorBindIP";
		public const string UI_NO_DEDICATED_CLIENTS = "F_UI_NoDedicatedClients";
	}
}
