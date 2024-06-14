using System;
using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.EssentialPatches
{
    /// <summary>
    /// Update version label with raid code when game started
    /// </summary>
    public class FikaVersionLabelUpdate_Patch : ModulePatch
    {
        public static string raidCode;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            if (!string.IsNullOrEmpty(raidCode))
            {
                Traverse preloaderUiTraverse = Traverse.Create(MonoBehaviourSingleton<PreloaderUI>.Instance);
                //Game version
                // preloaderUiTraverse.Field("string_2").SetValue($"Game version");
                //Raid code
                preloaderUiTraverse.Field("string_3").SetValue($"{raidCode}");
                //Game mode
                // preloaderUiTraverse.Field("string_4").SetValue("PvE");
                //Update version label
                preloaderUiTraverse.Method("method_6").GetValue();
            }
        }
    }
}