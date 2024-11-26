using EFT;
using System;
using System.Collections.Generic;

namespace Fika.Core.Utils
{
	/// <summary>
	/// Utilities for locales/language
	/// </summary>
	public static class LocaleUtils
	{
		private static readonly List<char> vowels = ['A', 'E', 'I', 'O', 'U'];

		/// <summary>
		/// Used to get the prefix of a word for EN locale, no longer used
		/// </summary>
		/// <param name="word"></param>
		/// <returns></returns>
		[Obsolete("No longer used", true)]
		public static string GetPrefix(string word)
		{
			char firstLetter = char.ToUpper(word[0]);

			if (vowels.Contains(firstLetter))
			{
				return "an";
			}

			return "a";
		}

		/// <summary>
		/// Used to determine whether this player was a boss
		/// </summary>
		/// <param name="wildSpawnType"></param>
		/// <returns>true if it's a boss, false if not</returns>
		public static bool IsBoss(WildSpawnType wildSpawnType, out string name)
		{
			name = string.Empty;
			switch (wildSpawnType)
			{
				case WildSpawnType.bossBoar:
					{
						name = "Kaban";
						break;
					}
				case WildSpawnType.bossBully:
					{
						name = "Reshala";
						break;
					}
				case WildSpawnType.bossGluhar:
					{
						name = "Glukhar";
						break;
					}
				case WildSpawnType.bossKilla:
					{
						name = "Killa";
						break;
					}
				case WildSpawnType.bossKnight:
					{
						name = "Knight";
						break;
					}
				case WildSpawnType.bossKojaniy:
					{
						name = "Shturman";
						break;
					}
				case WildSpawnType.bossSanitar:
					{
						name = "Sanitar";
						break;
					}
				case WildSpawnType.bossTagilla:
					{
						name = "Tagilla";
						break;
					}
				case WildSpawnType.bossZryachiy:
					{
						name = "Zryachiy";
						break;
					}
				case WildSpawnType.followerBigPipe:
					{
						name = "Big Pipe";
						break;
					}
				case WildSpawnType.followerBirdEye:
					{
						name = "Bird Eye";
						break;
					}
				case WildSpawnType.sectantPriest:
					{
						name = "Cultist Priest";
						break;
					}
				case WildSpawnType.bossKolontay:
					{
						name = "Kollontay";
						break;
					}
				case WildSpawnType.bossPartisan:
					{
						name = "Partisan";
						break;
					}
			}
			return !string.IsNullOrEmpty(name);
		}

		public const string RECEIVED_SHARED_QUEST_PROGRESS = "F_Client_ReceivedSharedQuestProgress";
		public const string RECEIVED_SHARED_ITEM_PICKUP = "F_Client_ReceivedSharedItemPickup";
		public const string RECEIVED_SHARED_ITEM_PLANT = "F_Client_ReceivedSharedItemPlant";
		public const string RECEIVE_PING = "F_Client_ReceivePing";
		public const string RECEIVE_PING_OBJECT = "F_Client_ReceivePingObject";
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
		public const string UI_WAIT_FOR_HOST_FINISH_INIT = "F_UI_WaitForHostFinishInit";
		public const string UI_WAIT_FOR_OTHER_PLAYERS = "F_UI_WaitForOtherPlayers";
		public const string UI_RETRIEVE_SPAWN_INFO = "F_UI_RetrieveSpawnInfo";
		public const string UI_RETRIEVE_LOOT = "F_UI_RetrieveLoot";
		public const string UI_WAIT_FOR_HOST_INIT = "F_UI_WaitForHostInit";
		public const string UI_RECONNECTING = "F_UI_Reconnecting";
		public const string UI_RETRIEVE_EXFIL_DATA = "F_UI_RetrieveExfilData";
		public const string UI_RETRIEVE_INTERACTABLES = "F_UI_RetrieveInteractables";
		public const string UI_INIT_COOP_GAME = "F_UI_InitCoopGame";
		public const string UI_WAIT_FOR_PLAYER = "F_UI_WaitForPlayer";
		public const string UI_WAIT_FOR_PLAYERS = "F_UI_WaitForPlayers";
		public const string UI_ALL_PLAYERS_JOINED = "F_UI_AllPlayersJoined";
		public const string UI_WAITING_FOR_CONNECT = "F_UI_WaitingForConnect";
		public const string UI_ERROR_CONNECTING_TO_RAID = "F_UI_ErrorConnectingToRaid";
		public const string UI_FINISHING_RAID_INIT = "F_UI_FinishingRaidInit";
		public const string UI_SYNC_THROWABLES = "F_UI_SyncThrowables";
		public const string UI_SYNC_INTERACTABLES = "F_UI_SyncInteractables";
		public const string UI_SYNC_LAMP_STATES = "F_UI_SyncLampStates";
		public const string UI_SYNC_WINDOWS = "F_UI_SyncWindows";
		public const string UI_RECEIVE_OWN_PLAYERS = "F_UI_ReceiveOwnPlayer";
		public const string UI_FINISH_RECONNECT = "F_UI_FinishReconnect";
		public const string FREECAM_ENABLED = "F_Client_FreeCamInputEnabled";
		public const string FREECAM_DISABLED = "F_Client_FreeCamInputDisabled";
		public const string UI_RAID_SETTINGS_DESCRIPTION = "F_UI_RaidSettingsDescription";
		public const string UI_COOP_GAME_MODE = "F_UI_CoopGameMode";
		public const string UI_COOP_RAID_SETTINGS = "F_UI_CoopRaidSettings";
		public const string UI_FIKA_ALWAYS_COOP = "F_UI_FikaAlwaysCoop";
		public const string UI_UPNP_FAILED = "F_UI_UpnpFailed";
		public const string UI_INIT_WEATHER = "F_UI_InitWeather";
		public const string UI_NOTIFICATION_STARTED_RAID = "F_Notification_RaidStarted";
		public const string UI_NOTIFICATION_RECEIVED_ITEM = "F_Notification_ItemReceived";
		public const string UI_NOTIFICATION_RAIDSETTINGS_DISABLED = "F_Notification_RaidSettingsDisabled";
	}
}
