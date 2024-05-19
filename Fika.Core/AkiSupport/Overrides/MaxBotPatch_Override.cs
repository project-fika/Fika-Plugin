using Aki.Common.Http;
using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using Fika.Core.Coop.Matchmaker;
using System.Reflection;

namespace Fika.Core.AkiSupport.Overrides
{
    internal class MaxBotPatch_Override : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            const string methodName = "SetSettings";
            var desiredType = PatchConstants.EftTypes.SingleCustom(x => x.GetMethod(methodName, flags) != null && IsTargetMethod(x.GetMethod(methodName, flags)));
            var desiredMethod = desiredType.GetMethod(methodName, flags);

            Logger.LogDebug($"{this.GetType().Name} Type: {desiredType?.Name}");
            Logger.LogDebug($"{this.GetType().Name} Method: {desiredMethod?.Name}");

            return desiredMethod;
        }

        private static bool IsTargetMethod(MethodInfo mi)
        {
            var parameters = mi.GetParameters();
            return parameters.Length == 3 && parameters[0].Name == "maxCount" && parameters[1].Name == "botPresets" && parameters[2].Name == "botScatterings";
        }

        [PatchPrefix]
        private static void PatchPreFix(ref int maxCount)
        {
            if (MatchmakerAcceptPatches.IsServer)
            {
                if (int.TryParse(RequestHandler.GetJson("/singleplayer/settings/bot/maxCap"), out int parsedMaxCount))
                {
                    Logger.LogWarning($"Set max bot cap to: {parsedMaxCount}");
                    maxCount = parsedMaxCount;
                }
                else
                {
                    Logger.LogWarning($"Unable to parse data from singleplayer/settings/bot/maxCap, using existing map max of {maxCount}");
                }
            }
            else
            {
                maxCount = 0;
            }
        }
    }
}
