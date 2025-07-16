using EFT;
using EFT.Interactive;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using Fika.Core.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Main.Patches
{
    public class GameWorld_ThrowItem_Patch : FikaPatch
    {
        private static readonly FieldInfo _networkPhysics = typeof(ObservedLootItem)
            .GetField("bool_3", BindingFlags.Instance | BindingFlags.NonPublic);

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethods()
                .First(x => x.Name == nameof(GameWorld.ThrowItem) && x.GetParameters().Length == 3);
        }

        [PatchPostfix]
        public static void Postfix(LootItem __result, IPlayer player)
        {
            if (__result is ObservedLootItem observedLootItem)
            {
                if (player.IsYourPlayer || player.IsAI)
                {
                    ItemPositionSyncer.Create(observedLootItem.gameObject, FikaBackendUtils.IsServer, observedLootItem);
                    return;
                }

                _networkPhysics.SetValue(observedLootItem, true);
            }
        }
    }
}
