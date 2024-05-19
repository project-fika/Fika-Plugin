// © 2024 Lacyway All Rights Reserved

using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.Matchmaker;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.UI
{
    /// <summary>
    /// Created by: Lacyway
    /// </summary>
    public class DisableReadyButton_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(MatchMakerSelectionLocationScreen).GetMethod("method_7", BindingFlags.Public | BindingFlags.Instance);

        [PatchPostfix]
        static void PatchPostfix()
        {
            var readyButton = GameObject.Find("ReadyButton");

            if (readyButton != null)
            {
                readyButton.SetActive(false);
                DefaultUIButton uiButton = readyButton.GetComponent<DefaultUIButton>();
                if (uiButton != null)
                {
                    uiButton.SetDisabledTooltip("Disabled with Fika");
                    uiButton.SetEnabledTooltip("Disabled with Fika");

                    if (uiButton.Interactable == true)
                    {
                        uiButton.Interactable = false;
                    }
                }
            }
        }
    }
}