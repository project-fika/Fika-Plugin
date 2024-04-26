using Aki.Common.Http;
using Aki.Reflection.Patching;
using EFT;
using Fika.Core.Coop.Matchmaker;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Session
{
    public class LocationDataRequest()
    {
        [JsonProperty("errmsg")]
        public string Errmsg { get; set; }
        [JsonProperty("err")]
        public int Err { get; set; }
        [JsonProperty("data")]
        public LocationSettingsClass.Location Data { get; set; }
    }

    public class LoadLocationLootPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(Class263).GetMethod(nameof(Class263.LoadLocationLoot));

        [PatchPrefix]
        private static bool PatchPrefix(string locationId, int variantId, ref Task<LocationSettingsClass.Location> __result)
        {
            if (MatchmakerAcceptPatches.MatchingType == EMatchmakerType.Single)
            {
                return true;
            }

            string serverId = MatchmakerAcceptPatches.GetGroupId();

            var objectToSend = new Dictionary<string, object>
            {
                { "locationId", locationId },
                { "variantId", variantId },
                { "serverId", serverId }
            };

            var converterType = typeof(AbstractGame).Assembly.GetTypes().First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null).
                GetField("Converters", BindingFlags.Static | BindingFlags.Public).GetValue(null) as JsonConverter[];

            __result = Task.Run(() =>
            {
                var result = RequestHandler.PostJson($"/coop/location/getLoot", JsonConvert.SerializeObject(objectToSend));
                var locationDataRequest = JsonConvert.DeserializeObject<LocationDataRequest>(result, new JsonSerializerSettings()
                {
                    Converters = converterType,
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    FloatFormatHandling = FloatFormatHandling.DefaultValue,
                    FloatParseHandling = FloatParseHandling.Double,
                    Error = (serializer, err) =>
                    {
                        Logger.LogError("Serialization Error: " + err);
                    }
                });

                if (locationDataRequest != null)
                    return locationDataRequest.Data;
                else
                    return null;
            });

            return __result == null;
        }
    }
}
