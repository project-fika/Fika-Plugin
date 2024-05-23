using Aki.Common.Http;
using EFT;
using Fika.Core.Models;
using Fika.Core.Networking.Http.Models;
using Fika.Core.UI.Models;
using Fuyu.Platform.Common.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
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

        private static byte[] EncodeBody<T>(T o)
        {
            var serialized = JsonConvert.SerializeObject(o);
            return Encoding.UTF8.GetBytes(serialized);
        }

        private static T DecodeBody<T>(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private static async Task<T> GetJsonAsync<T>(string path)
        {
            var response = await _httpClient.GetAsync(path);
            return DecodeBody<T>(response);
        }

        private static T GetJson<T>(string path)
        {
            return Task.Run(() => GetJsonAsync<T>(path)).Result;
        }

        private static async Task<T2> PostJsonAsync<T1, T2>(string path, T1 o)
        {
            var data = EncodeBody<T1>(o);
            var response = await _httpClient.PostAsync(path, data);
            return DecodeBody<T2>(response);
        }

        private static T2 PostJson<T1, T2>(string path, T1 o)
        {
            return Task.Run(() => PostJsonAsync<T1, T2>(path, o)).Result;
        }

        private static async Task<byte[]> PutJsonAsync<T>(string path, T o)
        {
            var data = EncodeBody(o);
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

        public static async Task UpdatePing(PingRequest data)
        {
            await PutJsonAsync("/fika/update/ping", data);
        }

        public static async Task UpdateSetStatus(SetStatusModel data)
        {
            await PutJsonAsync("/fika/update/setstatus", data);
        }

        public static async Task UpdateSpawnPoint(UpdateSpawnPointRequest data)
        {
            await PutJsonAsync("/fika/update/spawnpoint", data);
        }

        public static async Task UpdatePlayerSpawn(PlayerSpawnRequest data)
        {
            await PutJsonAsync("/fika/update/playerspawn", data);
        }

        public static void UpdateSetHost(SetHostRequest data)
        {
            PutJson("/fika/update/sethost", data);
        }

        public static async Task<SpawnPointResponse> RaidSpawnPoint(SpawnPointRequest data)
        {
            return await PostJsonAsync<SpawnPointRequest, SpawnPointResponse>("/fika/raid/spawnpoint", data);
        }

        public static void RaidLeave(PlayerLeftRequest data)
        {
            PutJson("/fika/raid/leave", data);
        }

        public static CreateMatch RaidJoin(MatchJoinRequest data)
        {
            return PostJson<MatchJoinRequest, CreateMatch>("/fika/raid/join", data);
        }

        public static void RaidCreate(CreateMatch data)
        {
            PutJson("/fika/raid/create", data);
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
    }
}