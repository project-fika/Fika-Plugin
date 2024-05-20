using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Airdrop;
using System.Linq;
using System.Reflection;

namespace Aki.Custom.Airdrops.Patches
{
    public class FikaAirdropFlare_Patch : ModulePatch
    {
        private static readonly string[] _usableFlares = ["624c09cfbc2e27219346d955", "62389ba9a63f32501b1b4451"];

        protected override MethodBase GetTargetMethod()
        {
            return typeof(FlareCartridge).GetMethod(nameof(FlareCartridge.Init),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        [PatchPostfix]
        private static void PatchPostfix(BulletClass flareCartridge)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            bool points = LocationScene.GetAll<AirdropPoint>().Any();

            if (gameWorld != null && points && _usableFlares.Any(x => x == flareCartridge.Template._id))
            {
                FikaAirdropsManager airdropsManager = gameWorld.gameObject.AddComponent<FikaAirdropsManager>();
                airdropsManager.isFlareDrop = true;
            }
        }
    }
}