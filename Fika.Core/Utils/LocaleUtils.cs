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
		public const string UI_MM_SESSION_SETTINGS_HEADER = "F_MMUI_SessionSettingsHeader";
		public const string UI_MM_SELECT_AMOUNT_DESCRIPTION = "F_MMUI_SelectAmountDescription";
		public const string UI_MM_USE_DEDICATED_HOST = "F_MMUI_UseDedicatedHost";
		public const string UI_MM_START_BUTTON = "F_MMUI_StartButton";
		public const string UI_MM_LOADING_HEADER = "F_MMUI_LoadingScreenHeader";
		public const string UI_MM_LOADING_DESCRIPTION = "F_MMUI_LoadingScreenDescription";
		public const string UI_MM_JOIN_AS_SPECTATOR = "F_MMUI_JoinAsSpectator";
		public const string UI_START_RAID = "F_UI_StartRaid";
		public const string UI_START_RAID_DESCRIPTION = "F_UI_StartRaidDescription";
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
		public const string UI_EXTRACT_MESSAGE = "F_UI_ExtractMessage";
		public const string UI_DOWNLOAD_PROFILE = "F_UI_DownloadProfile";
		public const string UI_LOCALE_ERROR_HEADER = "F_UI_LocaleError_H";
		public const string UI_LOCALE_ERROR_DESCRIPTION = "F_UI_LocaleError_D";

		// Main Menu UI
		public const string UI_MMUI_ONLINE_PLAYERS = "F_MMUI_OnlinePlayers";
		public const string UI_MMUI_IN_MENU = "F_MMUI_InMenu";
		public const string UI_MMUI_IN_RAID = "F_MMUI_InRaid";
		public const string UI_MMUI_IN_STASH = "F_MMUI_InStash";
		public const string UI_MMUI_IN_HIDEOUT = "F_MMUI_InHideout";
		public const string UI_MMUI_IN_FLEA = "F_MMUI_InFlea";
		public const string UI_MMUI_RAID_DETAILS = "F_MMUI_RaidDetails";

		// BepInEx

		public const string BEPINEX_H_ADVANCED = "F_BepInEx_H_Advanced";
		public const string BEPINEX_H_COOP = "F_BepInEx_H_Coop";
		public const string BEPINEX_H_COOP_NAME_PLATES = "F_BepInEx_H_Coop_NamePlates";
		public const string BEPINEX_H_COOP_QUEST_SHARING = "F_BepInEx_H_Coop_QuestSharing";
		public const string BEPINEX_H_COOP_PINGING = "F_BepInEx_H_Coop_Pinging";
		public const string BEPINEX_H_COOP_DEBUG = "F_BepInEx_H_Coop_Debug";
		public const string BEPINEX_H_PERFORMANCE = "F_BepInEx_H_Performance";
		public const string BEPINEX_H_PERFORMANCE_BOTS = "F_BepInEx_H_PerformanceBots";
		public const string BEPINEX_H_NETWORK = "F_BepInEx_H_Network";
		public const string BEPINEX_H_GAMEPLAY = "F_BepInEx_H_Gameplay";

		public const string BEPINEX_OFFICIAL_VERSION_T = "F_BepInEx_OfficialVersion_T";
		public const string BEPINEX_OFFICIAL_VERSION_D = "F_BepInEx_OfficialVersion_D";

		public const string BEPINEX_SHOW_FEED_T = "F_BepInEx_ShowFeed_T";
		public const string BEPINEX_SHOW_FEED_D = "F_BepInEx_ShowFeed_D";
		public const string BEPINEX_AUTO_EXTRACT_T = "F_BepInEx_AutoExtract_T";
		public const string BEPINEX_AUTO_EXTRACT_D = "F_BepInEx_AutoExtract_D";
		public const string BEPINEX_SHOW_EXTRACT_MESSAGE_T = "F_BepInEx_ShowExtractMessage_T";
		public const string BEPINEX_SHOW_EXTRACT_MESSAGE_D = "F_BepInEx_ShowExtractMessage_D";
		public const string BEPINEX_EXTRACT_KEY_T = "F_BepInEx_ExtractKey_T";
		public const string BEPINEX_EXTRACT_KEY_D = "F_BepInEx_ExtractKey_D";
		public const string BEPINEX_ENABLE_CHAT_T = "F_BepInEx_EnableChat_T";
		public const string BEPINEX_ENABLE_CHAT_D = "F_BepInEx_EnableChat_D";
		public const string BEPINEX_CHAT_KEY_T = "F_BepInEx_ChatKey_T";
		public const string BEPINEX_CHAT_KEY_D = "F_BepInEx_ChatKey_D";
		public const string BEPINEX_ENABLE_ONLINE_PLAYER_T = "F_BepInEx_EnableOnlinePlayers_T";
		public const string BEPINEX_ENABLE_ONLINE_PLAYER_D = "F_BepInEx_EnableOnlinePlayers_D";
		public const string BEPINEX_ONLINE_PLAYERS_SCALE_T = "F_BepInEx_OnlinePlayersScale_T";
		public const string BEPINEX_ONLINE_PLAYERS_SCALE_D = "F_BepInEx_OnlinePlayersScale_D";

		public const string BEPINEX_USE_NAME_PLATES_T = "F_BepInEx_UseNamePlates_T";
		public const string BEPINEX_USE_NAME_PLATES_D = "F_BepInEx_UseNamePlates_D";
		public const string BEPINEX_HIDE_HEALTH_BAR_T = "F_BepInEx_HideHealthBar_T";
		public const string BEPINEX_HIDE_HEALTH_BAR_D = "F_BepInEx_HideHealthBar_D";
		public const string BEPINEX_USE_PERCENT_T = "F_BepInEx_UsePercent_T";
		public const string BEPINEX_USE_PERCENT_D = "F_BepInEx_UsePercent_D";
		public const string BEPINEX_SHOW_EFFECTS_T = "F_BepInEx_ShowEffects_T";
		public const string BEPINEX_SHOW_EFFECTS_D = "F_BepInEx_ShowEffects_D";
		public const string BEPINEX_SHOW_FACTION_ICON_T = "F_BepInEx_ShowFactionIcon_T";
		public const string BEPINEX_SHOW_FACTION_ICON_D = "F_BepInEx_ShowFactionIcon_D";
		public const string BEPINEX_HIDE_IN_OPTIC_T = "F_BepInEx_HideInOptic_T";
		public const string BEPINEX_HIDE_IN_OPTIC_D = "F_BepInEx_HideInOptic_D";
		public const string BEPINEX_OPTIC_USE_ZOOM_T = "F_BepInEx_OpticUseZoom_T";
		public const string BEPINEX_OPTIC_USE_ZOOM_D = "F_BepInEx_OpticUseZoom_D";
		public const string BEPINEX_DEC_OPAC_PERI_T = "F_BepInEx_DecOpacPeri_T";
		public const string BEPINEX_DEC_OPAC_PERI_D = "F_BepInEx_DecOpacPeri_D";
		public const string BEPINEX_NAME_PLATE_SCALE_T = "F_BepInEx_NamePlateScale_T";
		public const string BEPINEX_NAME_PLATE_SCALE_D = "F_BepInEx_NamePlateScale_D";
		public const string BEPINEX_ADS_OPAC_T = "F_BepInEx_AdsOpac_T";
		public const string BEPINEX_ADS_OPAC_D = "F_BepInEx_AdsOpac_D";
		public const string BEPINEX_MAX_DISTANCE_T = "F_BepInEx_MaxDistance_T";
		public const string BEPINEX_MAX_DISTANCE_D = "F_BepInEx_MaxDistance_D";
		public const string BEPINEX_MIN_OPAC_T = "F_BepInEx_MinOpac_T";
		public const string BEPINEX_MIN_OPAC_D = "F_BepInEx_MinOpac_D";
		public const string BEPINEX_MIN_PLATE_SCALE_T = "F_BepInEx_MinPlateScale_T";
		public const string BEPINEX_MIN_PLATE_SCALE_D = "F_BepInEx_MinPlateScale_D";
		public const string BEPINEX_USE_OCCLUSION_T = "F_BepInEx_UseOcclusion_T";
		public const string BEPINEX_USE_OCCLUSION_D = "F_BepInEx_UseOcclusion_D";

		public const string BEPINEX_QUEST_TYPES_T = "F_BepInEx_QuestTypes_T";
		public const string BEPINEX_QUEST_TYPES_D = "F_BepInEx_QuestTypes_D";
		public const string BEPINEX_QS_NOTIFICATIONS_T = "F_BepInEx_QSNotifications_T";
		public const string BEPINEX_QS_NOTIFICATIONS_D = "F_BepInEx_QSNotifications_D";
		public const string BEPINEX_EASY_KILL_CONDITIONS_T = "F_BepInEx_EasyKillConditions_T";
		public const string BEPINEX_EASY_KILL_CONDITIONS_D = "F_BepInEx_EasyKillConditions_D";
		public const string BEPINEX_SHARED_KILL_XP_T = "F_BepInEx_SharedKillXP_T";
		public const string BEPINEX_SHARED_KILL_XP_D = "F_BepInEx_SharedKillXP_D";
		public const string BEPINEX_SHARED_BOSS_XP_T = "F_BepInEx_SharedBossXP_T";
		public const string BEPINEX_SHARED_BOSS_XP_D = "F_BepInEx_SharedBossXP_D";

		public const string BEPINEX_PING_SYSTEM_T = "F_BepInEx_PingSystem_T";
		public const string BEPINEX_PING_SYSTEM_D = "F_BepInEx_PingSystem_D";
		public const string BEPINEX_PING_BUTTON_T = "F_BepInEx_PingButton_T";
		public const string BEPINEX_PING_BUTTON_D = "F_BepInEx_PingButton_D";
		public const string BEPINEX_PING_COLOR_T = "F_BepInEx_PingColor_T";
		public const string BEPINEX_PING_COLOR_D = "F_BepInEx_PingColor_D";
		public const string BEPINEX_PING_SIZE_T = "F_BepInEx_PingSize_T";
		public const string BEPINEX_PING_SIZE_D = "F_BepInEx_PingSize_D";
		public const string BEPINEX_PING_TIME_T = "F_BepInEx_PingTime_T";
		public const string BEPINEX_PING_TIME_D = "F_BepInEx_PingTime_D";
		public const string BEPINEX_PING_ANIMATION_T = "F_BepInEx_PingAnimation_T";
		public const string BEPINEX_PING_ANIMATION_D = "F_BepInEx_PingAnimation_D";
		public const string BEPINEX_PING_OPTICS_T = "F_BepInEx_PingOptics_T";
		public const string BEPINEX_PING_OPTICS_D = "F_BepInEx_PingOptics_D";
		public const string BEPINEX_PING_OPTIC_ZOOM_T = "F_BepInEx_PingOpticZoom_T";
		public const string BEPINEX_PING_OPTIC_ZOOM_D = "F_BepInEx_PingOpticZoom_D";
		public const string BEPINEX_PING_SCALE_DISTANCE_T = "F_BepInEx_PingScaleDistance_T";
		public const string BEPINEX_PING_SCALE_DISTANCE_D = "F_BepInEx_PingScaleDistance_D";
		public const string BEPINEX_PING_MIN_OPAC_T = "F_BepInEx_PingMinOpac_T";
		public const string BEPINEX_PING_MIN_OPAC_D = "F_BepInEx_PingMinOpac_D";
		public const string BEPINEX_PING_RANGE_T = "F_BepInEx_PingRange_T";
		public const string BEPINEX_PING_RANGE_D = "F_BepInEx_PingRange_D";
		public const string BEPINEX_PING_SOUND_T = "F_BepInEx_PingSound_T";
		public const string BEPINEX_PING_SOUND_D = "F_BepInEx_PingSound_D";

		public const string BEPINEX_FREE_CAM_BUTTON_T = "F_BepInEx_FreeCamButton_T";
		public const string BEPINEX_FREE_CAM_BUTTON_D = "F_BepInEx_FreeCamButton_D";
		public const string BEPINEX_SPECTATE_BOTS_T = "F_BepInEx_SpectateBots_T";
		public const string BEPINEX_SPECTATE_BOTS_D = "F_BepInEx_SpectateBots_D";
		public const string BEPINEX_AZERTY_MODE_T = "F_BepInEx_AZERTYMode_T";
		public const string BEPINEX_AZERTY_MODE_D = "F_BepInEx_AZERTYMode_D";
		public const string BEPINEX_DRONE_MODE_T = "F_BepInEx_DroneMode_T";
		public const string BEPINEX_DRONE_MODE_D = "F_BepInEx_DroneMode_D";
		public const string BEPINEX_KEYBIND_OVERLAY_T = "F_BepInEx_KeybindOverlay_T";
		public const string BEPINEX_KEYBIND_OVERLAY_D = "F_BepInEx_KeybindOverlay_D";

		public const string BEPINEX_DYNAMIC_AI_T = "F_BepInEx_DynamicAI_T";
		public const string BEPINEX_DYNAMIC_AI_D = "F_BepInEx_DynamicAI_D";
		public const string BEPINEX_DYNAMIC_AI_RANGE_T = "F_BepInEx_DynamicAIRange_T";
		public const string BEPINEX_DYNAMIC_AI_RANGE_D = "F_BepInEx_DynamicAIRange_D";
		public const string BEPINEX_DYNAMIC_AI_RATE_T = "F_BepInEx_DynamicAIRate_T";
		public const string BEPINEX_DYNAMIC_AI_RATE_D = "F_BepInEx_DynamicAIRate_D";
		public const string BEPINEX_DYNAMIC_AI_NO_SNIPERS_T = "F_BepInEx_DynamicAINoSnipers_T";
		public const string BEPINEX_DYNAMIC_AI_NO_SNIPERS_D = "F_BepInEx_DynamicAINoSnipers_D";

		public const string BEPINEX_ENFORCED_SPAWN_LIMITS_T = "F_BepInEx_EnforcedSpawnLimits_T";
		public const string BEPINEX_ENFORCED_SPAWN_LIMITS_D = "F_BepInEx_EnforcedSpawnLimits_D";
		public const string BEPINEX_DESPAWN_FURTHEST_T = "F_BepInEx_DespawnFurthest_T";
		public const string BEPINEX_DESPAWN_FURTHEST_D = "F_BepInEx_DespawnFurthest_D";
		public const string BEPINEX_DESPAWN_MIN_DISTANCE_T = "F_BepInEx_DespawnMinDistance_T";
		public const string BEPINEX_DESPAWN_MIN_DISTANCE_D = "F_BepInEx_DespawnMinDistance_D";
		public const string BEPINEX_MAX_BOTS_T = "F_BepInEx_MaxBots_T";
		public const string BEPINEX_MAX_BOTS_D = "F_BepInEx_MaxBots_D";

		public const string BEPINEX_NATIVE_SOCKETS_T = "F_BepInEx_NativeSockets_T";
		public const string BEPINEX_NATIVE_SOCKETS_D = "F_BepInEx_NativeSockets_D";
		public const string BEPINEX_FORCE_IP_T = "F_BepInEx_ForceIP_T";
		public const string BEPINEX_FORCE_IP_D = "F_BepInEx_ForceIP_D";
		public const string BEPINEX_FORCE_BIND_IP_T = "F_BepInEx_ForceBindIP_T";
		public const string BEPINEX_FORCE_BIND_IP_D = "F_BepInEx_ForceBindIP_D";
		public const string BEPINEX_UDP_PORT_T = "F_BepInEx_UDPPort_T";
		public const string BEPINEX_UDP_PORT_D = "F_BepInEx_UDPPort_D";
		public const string BEPINEX_USE_UPNP_T = "F_BepInEx_UseUPnP_T";
		public const string BEPINEX_USE_UPNP_D = "F_BepInEx_UseUPnP_D";
		public const string BEPINEX_USE_NAT_PUNCH_T = "F_BepInEx_UseNatPunch_T";
		public const string BEPINEX_USE_NAT_PUNCH_D = "F_BepInEx_UseNatPunch_D";
		public const string BEPINEX_CONNECTION_TIMEOUT_T = "F_BepInEx_ConnectionTimeout_T";
		public const string BEPINEX_CONNECTION_TIMEOUT_D = "F_BepInEx_ConnectionTimeout_D";
		public const string BEPINEX_SEND_RATE_T = "F_BepInEx_SendRate_T";
		public const string BEPINEX_SEND_RATE_D = "F_BepInEx_SendRate_D";
		public const string BEPINEX_SMOOTHING_RATE_T = "F_BepInEx_SmoothingRate_T";
		public const string BEPINEX_SMOOTHING_RATE_D = "F_BepInEx_SmoothingRate_D";

		public const string BEPINEX_DISABLE_BOT_METABOLISM_T = "F_BepInEx_DisableBotMetabolism_T";
		public const string BEPINEX_DISABLE_BOT_METABOLISM_D = "F_BepInEx_DisableBotMetabolism_D";
	}
}
