using EFT;
using Fika.Core.Coop.Custom;
using Fika.Core.Models;
using Fika.Core.Networking.Models.Presence;
using Fika.Core.UI.Models;
using Fuyu.Platform.Common.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core.Networking.Http
{
	public static class FikaRequestHandler
	{
		private static readonly FuyuClient _httpClient;

		static FikaRequestHandler()
		{
			_httpClient = new FuyuClient(RequestHandler.Host, RequestHandler.SessionId);
		}

		public static async Task<IPAddress> GetPublicIP()
		{
			HttpClient client = _httpClient.GetClient();
			string ipString = await client.GetStringAsync("https://api.ipify.org/");
			ipString = ipString.Replace("\n", "");
			if (IPAddress.TryParse(ipString, out IPAddress ipAddress))
			{
				if (ipAddress.AddressFamily is AddressFamily.InterNetwork)
				{
					return ipAddress;
				}
				throw new ArgumentException($"IP address was not an IPv4 address, was: {ipAddress.AddressFamily}, address: {ipAddress}!");
			}
			ipString = await client.GetStringAsync("https://checkip.amazonaws.com/");
			if (IPAddress.TryParse(ipString, out ipAddress))
			{
				if (ipAddress.AddressFamily is AddressFamily.InterNetwork)
				{
					return ipAddress;
				}
				throw new ArgumentException($"IP address was not an IPv4 address, was: {ipAddress.AddressFamily}, address: {ipAddress}!");
			}
			ipString = await client.GetStringAsync("https://ipv4.icanhazip.com/");
			if (IPAddress.TryParse(ipString, out ipAddress))
			{
				if (ipAddress.AddressFamily is AddressFamily.InterNetwork)
				{
					return ipAddress;
				}
				throw new ArgumentException($"IP address was not an IPv4 address, was: {ipAddress.AddressFamily}, address: {ipAddress}!");
			}
			throw new Exception("Could not retrieve or parse the external address!");
		}

		private static byte[] EncodeBody<T>(T o)
		{
			string serialized = JsonConvert.SerializeObject(o);
			return Encoding.UTF8.GetBytes(serialized);
		}

		private static T DecodeBody<T>(byte[] data)
		{
			string json = Encoding.UTF8.GetString(data);
			return JsonConvert.DeserializeObject<T>(json);
		}

		private static async Task<T> GetJsonAsync<T>(string path)
		{
			byte[] response = await _httpClient.GetAsync(path);
			return DecodeBody<T>(response);
		}

		private static T GetJson<T>(string path)
		{
			return Task.Run(() => GetJsonAsync<T>(path)).Result;
		}

		private static async Task<T2> PostJsonAsync<T1, T2>(string path, T1 o)
		{
			byte[] data = EncodeBody<T1>(o);
			byte[] response = await _httpClient.PostAsync(path, data);
			return DecodeBody<T2>(response);
		}

		private static T2 PostJson<T1, T2>(string path, T1 o)
		{
			return Task.Run(() => PostJsonAsync<T1, T2>(path, o)).Result;
		}

		private static async Task<byte[]> PutJsonAsync<T>(string path, T o)
		{
			byte[] data = EncodeBody(o);
			return await _httpClient.PutAsync(path, data);
		}

		private static byte[] PutJson<T>(string path, T o)
		{
			return Task.Run(() => PutJsonAsync<T>(path, o)).Result;
		}

		public static BotDifficulties GetBotDifficulties()
		{
			return GetJson<BotDifficulties>("/singleplayer/settings/bot/difficulties/");
		}

		public static ClientConfigModel GetClientConfig()
		{
			return GetJson<ClientConfigModel>("/fika/client/config");
		}

		public static NatPunchServerConfigModel GetNatPunchServerConfig()
		{
			return GetJson<NatPunchServerConfigModel>("/fika/natpunchserver/config");
		}

		public static async Task UpdatePing(PingRequest data)
		{
			await PutJsonAsync("/fika/update/ping", data);
		}

		public static async Task UpdateSetStatus(SetStatusModel data)
		{
			await PutJsonAsync("/fika/update/setstatus", data);
		}

		public static async Task UpdatePlayerSpawn(PlayerSpawnRequest data)
		{
			await PutJsonAsync("/fika/update/playerspawn", data);
		}

		public static void UpdateSetHost(SetHostRequest data)
		{
			PutJson("/fika/update/sethost", data);
		}

		public static void RaidLeave(PlayerLeftRequest data)
		{
			PutJson("/fika/raid/leave", data);
		}

		public static CreateMatch RaidJoin(MatchJoinRequest data)
		{
			return PostJson<MatchJoinRequest, CreateMatch>("/fika/raid/join", data);
		}

		public static void UpdateAddPlayer(AddPlayerRequest data)
		{
			PutJson("/fika/update/addplayer", data);
		}

		public static async Task RaidCreate(CreateMatch data)
		{
			await PutJsonAsync("/fika/raid/create", data);
		}

		public static GetHostResponse GetHost(GetHostRequest data)
		{
			return PostJson<GetHostRequest, GetHostResponse>("/fika/raid/gethost", data);
		}

		public static LobbyEntry[] LocationRaids(RaidSettings data)
		{
			return PostJson<RaidSettings, LobbyEntry[]>("/fika/location/raids", data);
		}

		public static Dictionary<string, string> AvailableReceivers(AvailableReceiversRequest data)
		{
			return PostJson<AvailableReceiversRequest, Dictionary<string, string>>("/fika/senditem/availablereceivers", data);
		}

		public static async Task<RaidSettingsResponse> GetRaidSettings(RaidSettingsRequest data)
		{
			return await PostJsonAsync<RaidSettingsRequest, RaidSettingsResponse>("/fika/raid/getsettings", data);
		}

		public static async Task<JObject> GetProfile()
		{
			return await GetJsonAsync<JObject>("/fika/profile/download");
		}

		public static async Task<StartDedicatedResponse> StartDedicated(StartDedicatedRequest request)
		{
			return await PostJsonAsync<StartDedicatedRequest, StartDedicatedResponse>("/fika/raid/dedicated/start", request);
		}

		public static async Task<SetDedicatedStatusResponse> SetDedicatedStatus(SetDedicatedStatusRequest request)
		{
			return await PostJsonAsync<SetDedicatedStatusRequest, SetDedicatedStatusResponse>("/fika/raid/dedicated/status", request);
		}

		public static GetDedicatedStatusResponse GetDedicatedStatus()
		{
			return GetJson<GetDedicatedStatusResponse>("/fika/raid/dedicated/getstatus");
		}

		public static async Task RegisterPlayer(RegisterPlayerRequest request)
		{
			await PutJsonAsync("/fika/raid/registerPlayer", request);
		}

		public static async Task PlayerDied(AddPlayerRequest request)
		{
			await PutJsonAsync("/fika/update/playerdied", request);
		}

		public static CheckVersionResponse CheckServerVersion()
		{
			return GetJson<CheckVersionResponse>("/fika/client/check/version");
		}

		public static FikaPlayerPresence[] GetPlayerPresences()
		{
			return GetJson<FikaPlayerPresence[]>("/fika/presence/get");
		}

		public static void SetPresence(FikaSetPresence data)
		{
			PutJson("/fika/presence/set", data);
		}

		public static FikaPlayerPresence[] SetAndGetPresence(FikaSetPresence data)
		{
			return PostJson<FikaSetPresence, FikaPlayerPresence[]>("/fika/presence/setget", data);
		}
	}
}