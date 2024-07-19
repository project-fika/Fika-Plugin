using EFT;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.UI.Models;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.LocalGame
{
    /// <summary>
    /// Created by: Lacyway
    /// </summary>
    internal class TarkovApplication_LocalGamePreparer_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_38));

        [PatchPrefix]
        public static async void Prefix()
        {
            Logger.LogDebug("TarkovApplication_LocalGamePreparer_Patch:Prefix");

            if (FikaBackendUtils.IsSinglePlayer)
            {
                return;
            }

            bool isServer = FikaBackendUtils.IsServer;
            NetManagerUtils.CreateNetManager(FikaBackendUtils.IsServer);
            if (isServer)
            {
                NetManagerUtils.StartPinger();
            }
            await NetManagerUtils.InitNetManager(isServer);

            if (isServer)
            {
                SetStatusModel status = new(FikaBackendUtils.GetGroupId(), LobbyEntry.ELobbyStatus.COMPLETE);
                await FikaRequestHandler.UpdateSetStatus(status);
            }
        }
    }
}
