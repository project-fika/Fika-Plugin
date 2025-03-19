// © 2025 Lacyway All Rights Reserved

using Diz.Utils;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using JsonType;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Fika.Core.UI
{
    public static class FikaUIGlobals
    {
        private static readonly Dictionary<EColor, string> keyValuePairs = new()
        {
            { EColor.WHITE, "ffffff" },
            { EColor.BLACK, "000000" },
            { EColor.GREEN, "32a852" },
            { EColor.BROWN, "a87332" },
            { EColor.BLUE, "51c6db" },
            { EColor.RED, "a83232" }
        };

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

            obj.SetActive(true);
            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Bottom;
            text.margin = new Vector4(0, 0, 0, -350);
            text.text = overlayText;

            return text;
        }

        public static GClass3542 ShowFikaMessage(this PreloaderUI preloaderUI, string header, string message,
            ErrorScreen.EButtonType buttonType, float waitingTime, Action acceptCallback, Action endTimeCallback)
        {
            Traverse preloaderUiTraverse = Traverse.Create(preloaderUI);

            PreloaderUI.Class2821 messageHandler = new()
            {
                preloaderUI_0 = preloaderUI
            };

            if (!AsyncWorker.CheckIsMainThread())
            {
                FikaPlugin.Instance.FikaLogger.LogError("You are trying to show error screen from non-main thread!");
                return new GClass3542();
            }

            ErrorScreen errorScreenTemplate = preloaderUiTraverse.Field("_criticalErrorScreenTemplate").GetValue<ErrorScreen>();
            EmptyInputNode errorScreenContainer = preloaderUiTraverse.Field("_criticalErrorScreenContainer").GetValue<EmptyInputNode>();

            messageHandler.errorScreen = UnityEngine.Object.Instantiate(errorScreenTemplate, errorScreenContainer.transform, false);
            errorScreenContainer.AddChildNode(messageHandler.errorScreen);
            return messageHandler.errorScreen.ShowFikaMessage(header, message, acceptCallback, waitingTime, endTimeCallback, buttonType, true);
        }

        public static GClass3547 ShowFikaMessage(this ErrorScreen errorScreen, string title, string message,
            Action closeManuallyCallback = null, float waitingTime = 0f, Action timeOutCallback = null,
            ErrorScreen.EButtonType buttonType = ErrorScreen.EButtonType.OkButton, bool removeHtml = true)
        {
            Traverse errorScreenTraverse = Traverse.Create(errorScreen);

            ErrorScreen.Class2583 errorScreenHandler = new()
            {
                errorScreen_0 = errorScreen
            };
            if (!MonoBehaviourSingleton<PreloaderUI>.Instance.CanShowErrorScreen)
            {
                return new GClass3547();
            }
            if (removeHtml)
            {
                message = ErrorScreen.smethod_0(message);
            }
            ItemUiContext.Instance.CloseAllWindows();

            Action action_1 = timeOutCallback ?? closeManuallyCallback;
            errorScreenTraverse.Field("action_1").SetValue(action_1);
            MethodBase baseShow = typeof(ErrorScreen).BaseType.GetMethod("Show");

            errorScreenHandler.context = (GClass3547)baseShow.Invoke(errorScreen, [closeManuallyCallback]);
            errorScreenHandler.context.OnAccept += errorScreen.method_3;
            errorScreenHandler.context.OnDecline += errorScreen.method_4;
            errorScreenHandler.context.OnCloseSilent += errorScreen.method_4;

            CompositeDisposableClass ui = Traverse.Create(errorScreen).Field<CompositeDisposableClass>("UI").Value;

            ui.AddDisposable(errorScreenHandler.method_0);
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

            TextMeshProUGUI errorDescription = Traverse.Create(errorScreen).Field<TextMeshProUGUI>("_errorDescription").Value;
            errorDescription.text = string_1;

            Coroutine coroutine_0 = errorScreenTraverse.Field("coroutine_0").GetValue<Coroutine>();
            if (coroutine_0 != null)
            {
                errorScreen.StopCoroutine(coroutine_0);
            }
            if (waitingTime > 0f)
            {
                errorScreenTraverse.Field("coroutine_0").SetValue(errorScreen.StartCoroutine(errorScreen.method_2(EFTDateTimeClass.Now.AddSeconds((double)waitingTime))));
            }
            return errorScreenHandler.context;
        }

        private static string GetHexByColor(EColor color)
        {
            return keyValuePairs.TryGetValue(color, out string value) ? value : "ffffff";
        }

        /// <summary>
        /// Utility used to color text within a <see cref="CustomTextMeshProUGUI"/>
        /// </summary>
        /// <param name="color">The color to return the text in</param>
        /// <param name="text">The original text</param>
        /// <returns>Text in color</returns>
        public static string ColorizeText(EColor color, string text)
        {
            return $"<color=#{GetHexByColor(color)}>{text}</color>";
        }

        /// <summary>
        /// Utility used to color text within a <see cref="CustomTextMeshProUGUI"/>
        /// </summary>
        /// <param name="text">The original text</param>
        /// <returns>Text in bold</returns>
        public static string BoldText(string text)
        {
            return $"<b>{text}</b>";
        }

        public static DateTime StaticTime
        {
            get
            {
                return new DateTime(2016, 8, 4, 15, 28, 0);
            }
        }

        public static string FormattedTime(EDateTime time, bool staticTime)
        {
            if (TarkovApplication.Exist(out TarkovApplication tarkovApplication))
            {
                if (tarkovApplication.Session != null)
                {
                    ISession session = tarkovApplication.Session;

                    if (staticTime)
                    {
                        DateTime staticDate = StaticTime;
                        return time == EDateTime.CURR ? staticDate.ToString("HH:mm:ss") : staticDate.AddHours(-12).ToString("HH:mm:ss");
                    }

                    DateTime backendTime = session.GetCurrentLocationTime;
                    if (backendTime == DateTime.MinValue)
                    {
                        return "";
                    }
                    return time == EDateTime.CURR ? backendTime.ToString("HH:mm:ss") : backendTime.AddHours(-12).ToString("HH:mm:ss");
                }
            }
            return "";
        }

        public enum EFikaPlayerPresence
        {
            IN_MENU,
            IN_RAID,
            IN_STASH,
            IN_HIDEOUT,
            IN_FLEA
        }

        /// <summary>
        /// Enum used for <see cref="ColorizeText"/>
        /// </summary>
        public enum EColor
        {
            WHITE,
            BLACK,
            GREEN,
            BROWN,
            BLUE,
            RED
        }
    }
}
