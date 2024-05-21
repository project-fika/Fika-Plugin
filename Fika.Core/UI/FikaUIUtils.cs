// © 2024 Lacyway All Rights Reserved

using Diz.Utils;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Fika.Core.UI
{
    internal static class FikaUIUtils
    {
        public static TextMeshProUGUI CreateOverlayText(string overlayText)
        {
            GameObject obj = GameObject.Find("/Preloader UI/Preloader UI/Watermark");
            GameObject labelObj = GameObject.Find("/Preloader UI/Preloader UI/Watermark/Label");

            if (labelObj != null)
            {
                UnityEngine.Object.Destroy(labelObj);
            }

            ClientWatermark watermarkText = obj.GetComponent<ClientWatermark>();
            if (watermarkText != null)
            {
                UnityEngine.Object.Destroy(watermarkText);
            }

            obj.active = true;
            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Bottom;
            text.margin = new Vector4(0, 0, 0, -350);
            text.text = overlayText;

            return text;
        }

        public static GClass3085 ShowFikaMessage(this PreloaderUI preloaderUI, string header, string message,
            ErrorScreen.EButtonType buttonType, float waitingTime, Action acceptCallback, Action endTimeCallback)
        {
            Traverse preloaderUiTraverse = Traverse.Create(preloaderUI);

            PreloaderUI.Class2561 messageHandler = new()
            {
                preloaderUI_0 = preloaderUI,
                acceptCallback = acceptCallback,
                endTimeCallback = endTimeCallback
            };

            if (!AsyncWorker.CheckIsMainThread())
            {
                FikaPlugin.Instance.FikaLogger.LogError("You are trying to show error screen from non-main thread!");
                return new GClass3085();
            }

            ErrorScreen errorScreenTemplate = preloaderUiTraverse.Field("_criticalErrorScreenTemplate").GetValue<ErrorScreen>();
            EmptyInputNode errorScreenContainer = preloaderUiTraverse.Field("_criticalErrorScreenContainer").GetValue<EmptyInputNode>();

            messageHandler.errorScreen = UnityEngine.Object.Instantiate(errorScreenTemplate, errorScreenContainer.transform, false);
            errorScreenContainer.AddChildNode(messageHandler.errorScreen);
            return messageHandler.errorScreen.ShowFikaMessage(header, message, new Action(messageHandler.method_1), waitingTime, new Action(messageHandler.method_2), buttonType, true);
        }

        public static GClass3087 ShowFikaMessage(this ErrorScreen errorScreen, string title, string message,
            Action closeManuallyCallback = null, float waitingTime = 0f, Action timeOutCallback = null,
            ErrorScreen.EButtonType buttonType = ErrorScreen.EButtonType.OkButton, bool removeHtml = true)
        {
            Traverse errorScreenTraverse = Traverse.Create(errorScreen);

            ErrorScreen.Class2352 errorScreenHandler = new()
            {
                errorScreen_0 = errorScreen
            };
            if (!MonoBehaviourSingleton<PreloaderUI>.Instance.CanShowErrorScreen)
            {
                return new GClass3087();
            }
            if (removeHtml)
            {
                message = ErrorScreen.smethod_0(message);
            }
            ItemUiContext.Instance.CloseAllWindows();

            Action action_1 = timeOutCallback ?? closeManuallyCallback;
            errorScreenTraverse.Field("action_1").SetValue(action_1);
            MethodBase baseShow = typeof(ErrorScreen).BaseType.GetMethod("Show");

            errorScreenHandler.context = (GClass3087)baseShow.Invoke(errorScreen, [closeManuallyCallback]);
            errorScreenHandler.context.OnAccept += errorScreen.method_3;
            errorScreenHandler.context.OnDecline += errorScreen.method_4;
            errorScreenHandler.context.OnCloseSilent += errorScreen.method_4;

            GClass767 ui = Traverse.Create(errorScreen).Field("UI").GetValue<GClass767>();

            ui.AddDisposable(new Action(errorScreenHandler.method_0));
            string text = buttonType switch
            {
                ErrorScreen.EButtonType.OkButton => "I UNDERSTAND",
                ErrorScreen.EButtonType.CancelButton => "CANCEL",
                ErrorScreen.EButtonType.QuitButton => "I DECLINE",
                _ => throw new ArgumentOutOfRangeException()
            };

            DefaultUIButton exitButton = errorScreenTraverse.Field("_exitButton").GetValue<DefaultUIButton>();

            exitButton.SetHeaderText(text, exitButton.HeaderSize);
            errorScreen.RectTransform.anchoredPosition = Vector2.zero;

            errorScreen.Caption.SetText(string.IsNullOrEmpty(title) ? "ERROR" : title);

            string string_1 = message.SubstringIfNecessary(500);
            errorScreenTraverse.Field("string_1").SetValue(string_1);

            TextMeshProUGUI errorDescription = Traverse.Create(errorScreen).Field("_errorDescription").GetValue<TextMeshProUGUI>();
            errorDescription.text = string_1;

            Coroutine coroutine_0 = errorScreenTraverse.Field("coroutine_0").GetValue<Coroutine>();
            if (coroutine_0 != null)
            {
                errorScreen.StopCoroutine(coroutine_0);
            }
            if (waitingTime > 0f)
            {
                errorScreenTraverse.Field("coroutine_0").SetValue(errorScreen.StartCoroutine(errorScreen.method_2(GClass1296.Now.AddSeconds((double)waitingTime))));
            }
            return errorScreenHandler.context;
        }
    }
}
