// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections.Generic;
using System.Reflection;
using Diz.Utils;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using Fika.Core.Main.Utils;
using HarmonyLib;
using JsonType;
using TMPro;

namespace Fika.Core.UI;

/// <summary>
/// Provides global UI utilities and helpers
/// </summary>
public static class FikaUIGlobals
{
    /// <summary>
    /// Dictionary mapping <see cref="EColor"/> values to their corresponding hex color codes.
    /// </summary>
    private static readonly Dictionary<EColor, string> _keyValuePairs = new()
    {
        { EColor.WHITE, "ffffff" },
        { EColor.BLACK, "000000" },
        { EColor.GREEN, "32a852" },
        { EColor.BROWN, "a87332" },
        { EColor.BLUE, "51c6db" },
        { EColor.RED, "a83232" }
    };

    /// <summary>
    /// Creates an overlay text element in the UI with the specified text.
    /// </summary>
    /// <param name="overlayText">The text to display in the overlay.</param>
    /// <returns>The created <see cref="TextMeshProUGUI"/> component.</returns>
    public static TextMeshProUGUI CreateOverlayText(string overlayText)
    {
        var obj = GameObject.Find("/Preloader UI/Preloader UI/Watermark");
        var labelObj = GameObject.Find("/Preloader UI/Preloader UI/Watermark/Label");

        if (labelObj != null)
        {
            UnityEngine.Object.Destroy(labelObj);
        }

        var watermarkText = obj.GetComponent<ClientWatermark>();
        if (watermarkText != null)
        {
            UnityEngine.Object.Destroy(watermarkText);
        }

        obj.SetActive(true);
        var text = obj.AddComponent<TextMeshProUGUI>();
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Bottom;
        text.margin = new Vector4(0, 0, 0, -350);
        text.text = overlayText;

        return text;
    }

    /// <summary>
    /// Shows a Fika message using the <see cref="PreloaderUI"/> error screen.
    /// </summary>
    /// <param name="preloaderUI">The preloader UI instance.</param>
    /// <param name="header">The header text for the message.</param>
    /// <param name="message">The message body text.</param>
    /// <param name="buttonType">The type of button to display.</param>
    /// <param name="waitingTime">The time to wait before auto-closing the message.</param>
    /// <param name="acceptCallback">Callback to invoke when the message is accepted.</param>
    /// <param name="endTimeCallback">Callback to invoke when the waiting time ends.</param>
    /// <returns>The context object for the displayed message.</returns>
    public static GClass3835 ShowFikaMessage(this PreloaderUI preloaderUI, string header, string message,
        ErrorScreen.EButtonType buttonType, float waitingTime, Action acceptCallback, Action endTimeCallback)
    {
        var preloaderUiTraverse = Traverse.Create(preloaderUI);

        PreloaderUI.Class2988 messageHandler = new()
        {
            preloaderUI_0 = preloaderUI
        };

        if (!AsyncWorker.CheckIsMainThread())
        {
            FikaGlobals.LogError("You are trying to show error screen from non-main thread!");
            return new GClass3835();
        }

        var errorScreenTemplate = preloaderUiTraverse.Field("_criticalErrorScreenTemplate").GetValue<ErrorScreen>();
        var errorScreenContainer = preloaderUiTraverse.Field("_criticalErrorScreenContainer").GetValue<EmptyInputNode>();

        messageHandler.errorScreen = UnityEngine.Object.Instantiate(errorScreenTemplate, errorScreenContainer.transform, false);
        errorScreenContainer.AddChildNode(messageHandler.errorScreen);
        return messageHandler.errorScreen.ShowFikaMessage(header, message, acceptCallback, waitingTime, endTimeCallback, buttonType, true);
    }

    /// <summary>
    /// Shows a Fika message using the <see cref="ErrorScreen"/> UI.
    /// </summary>
    /// <param name="errorScreen">The error screen instance.</param>
    /// <param name="title">The title of the error message.</param>
    /// <param name="message">The message body text.</param>
    /// <param name="closeManuallyCallback">Callback to invoke when the message is closed manually.</param>
    /// <param name="waitingTime">The time to wait before auto-closing the message.</param>
    /// <param name="timeOutCallback">Callback to invoke when the waiting time ends.</param>
    /// <param name="buttonType">The type of button to display.</param>
    /// <param name="removeHtml">Whether to remove HTML tags from the message.</param>
    /// <returns>The context object for the displayed message.</returns>
    public static GClass3835 ShowFikaMessage(this ErrorScreen errorScreen, string title, string message,
        Action closeManuallyCallback = null, float waitingTime = 0f, Action timeOutCallback = null,
        ErrorScreen.EButtonType buttonType = ErrorScreen.EButtonType.OkButton, bool removeHtml = true)
    {
        var errorScreenTraverse = Traverse.Create(errorScreen);

        ErrorScreen.Class2741 errorScreenHandler = new()
        {
            errorScreen_0 = errorScreen
        };
        if (!MonoBehaviourSingleton<PreloaderUI>.Instance.CanShowErrorScreen)
        {
            return new GClass3835();
        }
        if (removeHtml)
        {
            message = ErrorScreen.smethod_0(message);
        }
        ItemUiContext.Instance.CloseAllWindows();

        var action_1 = timeOutCallback ?? closeManuallyCallback;
        errorScreenTraverse.Field("action_1").SetValue(action_1);
        MethodBase baseShow = typeof(ErrorScreen).BaseType.GetMethod("Show");

        errorScreenHandler.context = (GClass3835)baseShow.Invoke(errorScreen, []);
        errorScreenHandler.context.OnAccept += errorScreen.method_3;
        if (timeOutCallback != null)
        {
            errorScreenHandler.context.OnAccept += timeOutCallback;
        }
        errorScreenHandler.context.OnDecline += errorScreen.method_4;
        errorScreenHandler.context.OnDecline += Application.Quit;
        errorScreenHandler.context.OnCloseSilent += errorScreen.method_4;

        var ui = Traverse.Create(errorScreen).Field<CompositeDisposableClass>("UI").Value;

        ui.AddDisposable(errorScreenHandler.method_0);
        var text = buttonType switch
        {
            ErrorScreen.EButtonType.OkButton => "I UNDERSTAND",
            ErrorScreen.EButtonType.CancelButton => "CANCEL",
            ErrorScreen.EButtonType.QuitButton => "I DECLINE",
            _ => throw new ArgumentOutOfRangeException()
        };

        var exitButton = errorScreenTraverse.Field("_exitButton").GetValue<DefaultUIButton>();

        exitButton.SetHeaderText(text, exitButton.HeaderSize);
        errorScreen.RectTransform.anchoredPosition = Vector2.zero;

        errorScreen.Caption.SetText(string.IsNullOrEmpty(title) ? "ERROR" : title);

        var string_1 = message.SubstringIfNecessary(500);
        errorScreenTraverse.Field("string_1").SetValue(string_1);

        var errorDescription = Traverse.Create(errorScreen).Field<TextMeshProUGUI>("_errorDescription").Value;
        errorDescription.text = string_1;

        var coroutine_0 = errorScreenTraverse.Field("coroutine_0").GetValue<Coroutine>();
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

    /// <summary>
    /// Gets the hex color code for the specified <see cref="EColor"/>.
    /// </summary>
    /// <param name="color">The color enum value.</param>
    /// <returns>The hex color code as a string.</returns>
    private static string GetHexByColor(EColor color)
    {
        return _keyValuePairs.TryGetValue(color, out var value) ? value : "ffffff";
    }

    /// <summary>
    /// Utility used to color text within a <see cref="CustomTextMeshProUGUI"/>.
    /// </summary>
    /// <param name="color">The color to return the text in.</param>
    /// <param name="text">The original text.</param>
    /// <returns>Text in color.</returns>
    public static string ColorizeText(EColor color, string text)
    {
        return $"<color=#{GetHexByColor(color)}>{text}</color>";
    }

    /// <summary>
    /// Utility used to make text bold within a <see cref="CustomTextMeshProUGUI"/>.
    /// </summary>
    /// <param name="text">The original text.</param>
    /// <returns>Text in bold.</returns>
    public static string BoldText(string text)
    {
        return $"<b>{text}</b>";
    }

    /// <summary>
    /// Gets a static date and time value used for certain UI displays.
    /// </summary>
    public static DateTime StaticTime
    {
        get
        {
            return new DateTime(2016, 8, 4, 15, 28, 0);
        }
    }

    /// <summary>
    /// Returns a formatted time string based on the specified <see cref="EDateTime"/> and static time flag.
    /// </summary>
    /// <param name="time">The time type to format (current or previous).</param>
    /// <param name="staticTime">Whether to use the static time or backend time.</param>
    /// <returns>The formatted time string, or an empty string if unavailable.</returns>
    public static string FormattedTime(EDateTime time, bool staticTime)
    {
        if (TarkovApplication.Exist(out var tarkovApplication))
        {
            if (tarkovApplication.Session != null)
            {
                var session = tarkovApplication.Session;

                if (staticTime)
                {
                    var staticDate = StaticTime;
                    return time == EDateTime.CURR ? staticDate.ToString("HH:mm:ss") : staticDate.AddHours(-12).ToString("HH:mm:ss");
                }

                var backendTime = session.GetCurrentLocationTime;
                if (backendTime == DateTime.MinValue)
                {
                    return "";
                }
                return time == EDateTime.CURR ? backendTime.ToString("HH:mm:ss") : backendTime.AddHours(-12).ToString("HH:mm:ss");
            }
        }
        return "";
    }

    /// <summary>
    /// Represents the presence state of a Fika player.
    /// </summary>
    public enum EFikaPlayerPresence
    {
        /// <summary>
        /// Player is in the menu.
        /// </summary>
        IN_MENU,
        /// <summary>
        /// Player is in a raid.
        /// </summary>
        IN_RAID,
        /// <summary>
        /// Player is in the stash.
        /// </summary>
        IN_STASH,
        /// <summary>
        /// Player is in the hideout.
        /// </summary>
        IN_HIDEOUT,
        /// <summary>
        /// Player is in the flea market.
        /// </summary>
        IN_FLEA
    }

    /// <summary>
    /// Enum used for <see cref="ColorizeText"/> to specify text color.
    /// </summary>
    public enum EColor
    {
        /// <summary>
        /// White color.
        /// </summary>
        WHITE,
        /// <summary>
        /// Black color.
        /// </summary>
        BLACK,
        /// <summary>
        /// Green color.
        /// </summary>
        GREEN,
        /// <summary>
        /// Brown color.
        /// </summary>
        BROWN,
        /// <summary>
        /// Blue color.
        /// </summary>
        BLUE,
        /// <summary>
        /// Red color.
        /// </summary>
        RED
    }
}
