using BepInEx.Logging;
using Comfort.Common;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Coop.Utils
{
	public static class NetManagerUtils
	{
		private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("NetManagerUtils");
		public static GameObject FikaGameObject;

		public static void CreateFikaGameObject()
		{
			FikaGameObject = new GameObject("FikaGameObject");
			GameObject.DontDestroyOnLoad(FikaGameObject);
			logger.LogInfo("FikaGameObject has been created!");
		}

		public static void CreateNetManager(bool isServer)
		{
			if (FikaGameObject == null)
			{
				CreateFikaGameObject();
			}

			if (isServer)
			{
				FikaServer server = FikaGameObject.AddComponent<FikaServer>();
				Singleton<FikaServer>.Create(server);
				logger.LogInfo("FikaServer has started!");
				Singleton<IFikaNetworkManager>.Create(server);
			}
			else
			{
				FikaClient client = FikaGameObject.AddComponent<FikaClient>();
				Singleton<FikaClient>.Create(client);
				logger.LogInfo("FikaClient has started!");
				Singleton<IFikaNetworkManager>.Create(client);
			}
		}

		public static void CreatePingingClient()
		{
			if (FikaGameObject == null)
			{
				CreateFikaGameObject();
			}

			FikaPingingClient pingingClient = FikaGameObject.AddComponent<FikaPingingClient>();
			Singleton<FikaPingingClient>.Create(pingingClient);
			logger.LogInfo("FikaPingingClient has started!");
		}

		public static void DestroyNetManager(bool isServer)
		{
			if (FikaBackendUtils.IsTransit)
			{
				Singleton<IFikaNetworkManager>.Instance.CoopHandler.CleanUpForTransit();
				if (isServer)
				{
					FikaServer server = Singleton<FikaServer>.Instance;
					server.RaidInitialized = false;
					server.ReadyClients = 0;
					return;
				}

				FikaClient client = Singleton<FikaClient>.Instance;
				client.HostReady = false;
				client.HostLoaded = false;
				client.ReadyClients = 0;
				return;
			}

			FikaBackendUtils.MatchingType = EMatchmakerType.Single;

			if (FikaGameObject != null)
			{
				if (isServer)
				{
					FikaServer server = Singleton<FikaServer>.Instance;
					if (!Singleton<IFikaNetworkManager>.TryRelease(server))
					{
						logger.LogError("Unable to release Server from Singleton!");
					}
					try
					{
						server.PrintStatistics();
						server.NetServer.Stop();
					}
					catch (Exception ex)
					{
						logger.LogError("DestroyNetManager: " + ex.Message);
					}
					Singleton<FikaServer>.TryRelease(server);
					GameObject.Destroy(server);
					logger.LogInfo("Destroyed FikaServer");
				}
				else
				{
					FikaClient client = Singleton<FikaClient>.Instance;
					if (!Singleton<IFikaNetworkManager>.TryRelease(client))
					{
						logger.LogError("Unable to release Client from Singleton!");
					}
					try
					{
						client.PrintStatistics();
						client.NetClient.Stop();
					}
					catch (Exception ex)
					{
						logger.LogError("DestroyNetManager: " + ex.Message);
					}
					Singleton<FikaClient>.TryRelease(client);
					GameObject.Destroy(client);
					logger.LogInfo("Destroyed FikaClient");
				}
			}
		}

		public static void DestroyPingingClient()
		{
			if (FikaGameObject != null)
			{
				FikaPingingClient pingingClient = Singleton<FikaPingingClient>.Instance;
				pingingClient.StopKeepAliveRoutine();
				pingingClient.NetClient.Stop();
				Singleton<FikaPingingClient>.TryRelease(pingingClient);
				GameObject.Destroy(pingingClient);
				logger.LogInfo("Destroyed FikaPingingClient");
			}
		}

		public static Task InitNetManager(bool isServer)
		{
			if (FikaGameObject != null)
			{
				if (isServer)
				{
					FikaServer server = Singleton<FikaServer>.Instance;
					if (!server.Started)
					{
						return server.Init();
					}
					return Task.CompletedTask;
				}
				else
				{
					FikaClient client = Singleton<FikaClient>.Instance;
					if (!client.Started)
					{
						client.Init();
					}
					return Task.CompletedTask;
				}
			}

			logger.LogError("InitNetManager: FikaGameObject was null!");
			throw new NullReferenceException("FikaGameObject was null");
		}

		public static Task SetupGameVariables(bool isServer, CoopPlayer coopPlayer)
		{
			if (isServer)
			{
				Singleton<FikaServer>.Instance.SetupGameVariables(coopPlayer);
			}
			else
			{
				Singleton<FikaClient>.Instance.SetupGameVariables(coopPlayer);
			}

			return Task.CompletedTask;
		}

		public static void StartPinger()
		{
			if (FikaGameObject != null)
			{
				FikaPinger fikaPinger = FikaGameObject.AddComponent<FikaPinger>();
				fikaPinger.StartPingRoutine();
			}
		}

		public static void StopPinger()
		{
			if (FikaGameObject != null)
			{
				FikaPinger fikaPinger = FikaGameObject.GetComponent<FikaPinger>();
				if (fikaPinger != null)
				{
					GameObject.Destroy(fikaPinger);
				}
				else
				{
					logger.LogError("StopPinger: Could not find FikaPinger!");
				}
			}
		}

		public static Task CreateCoopHandler()
		{
			if (FikaBackendUtils.IsTransit)
			{
				return Task.CompletedTask;
			}

			logger.LogInfo("Creating CoopHandler...");
			IFikaNetworkManager networkManager = Singleton<IFikaNetworkManager>.Instance;
			if (networkManager != null)
			{
				if (FikaGameObject != null)
				{
					CoopHandler coopHandler = FikaGameObject.AddComponent<CoopHandler>();
					networkManager.CoopHandler = coopHandler;

					if (!string.IsNullOrEmpty(FikaBackendUtils.GroupId))
					{
						coopHandler.ServerId = FikaBackendUtils.GroupId;
						return Task.CompletedTask;
					}

					GameObject.Destroy(coopHandler);
					logger.LogError("No ServerId found, deleting CoopHandler!");
					throw new MissingReferenceException("No Server Id found");
				}
			}

			return Task.FromException(new NullReferenceException("CreateCoopHandler: IFikaNetworkManager or FikaGameObject was null"));
		}
	}
}