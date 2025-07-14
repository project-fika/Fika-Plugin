using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using HarmonyLib;
using LiteNetLib.Utils;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Custom
{
    internal class FikaChat : MonoBehaviour
    {
        private Rect _windowRect;
        private string _nickname;
        private string _chatText;
        private string _textField;
        private string _textFieldName;
        private bool _show;
        private bool _selectText;
        private bool _isServer;
        private NetDataWriter _writer;
        private UISoundsWrapper _soundsWrapper;
        private InputManager _manager;

        private static readonly FieldInfo _showMouseField = typeof(InputManager).GetField("bool_2",
            BindingFlags.Instance | BindingFlags.NonPublic);

        protected void Awake()
        {
            _windowRect = new(20, Screen.height - 260, 600, 250);
            _nickname = FikaBackendUtils.PMCName;
            _chatText = string.Empty;
            _textField = string.Empty;
            _textFieldName = "TextField";
            _show = false;
            _isServer = FikaBackendUtils.IsServer;
            _writer = new();
            GUISounds guiSounds = Singleton<GUISounds>.Instance;
            _soundsWrapper = Traverse.Create(guiSounds).Field<UISoundsWrapper>("uisoundsWrapper_0").Value;
            GameObject managerObj = GameObject.Find("___Input");
            if (managerObj != null)
            {
                InputManager inputManager = managerObj.GetComponent<InputManager>();
                if (inputManager != null)
                {
                    _manager = inputManager;
                }
                else
                {
                    FikaGlobals.LogError("Could not find InputManager on the manager object");
                }
                return;
            }
            FikaGlobals.LogError("Could not find ___Input game object");
        }

        protected void Update()
        {
            if (Input.GetKeyDown(FikaPlugin.ChatKey.Value.MainKey))
            {
                ToggleVisibility();
            }
        }

        protected void OnGUI()
        {
            if (!_show)
            {
                return;
            }

            GUI.skin.label.alignment = TextAnchor.LowerLeft;
            GUI.skin.window.alignment = TextAnchor.UpperLeft;
            GUI.Window(0, _windowRect, DrawWindow, "Fika Chat");

            if (Event.current.isKey)
            {
                if (Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter && _show)
                {
                    SendMessage();
                    GUI.UnfocusWindow();
                    GUI.FocusControl(null);
                }
            }
        }

        private void ToggleVisibility()
        {
            _show = !_show;

            if (_show)
            {
                if (_manager != null)
                {
                    GamePlayerOwner.SetIgnoreInput(true);
                    _showMouseField.SetValue(_manager, true);
                }
                _selectText = true;
            }
            else
            {
                if (_manager != null)
                {
                    GamePlayerOwner.SetIgnoreInput(false);
                    GamePlayerOwner.MyPlayer.MovementContext.IsAxesIgnored = false;
                }
                _showMouseField.SetValue(_manager, false);
            }
        }

        private void SendMessage()
        {
            if (!_show)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_textField))
            {
                string message = _textField;
                _textField = string.Empty;

                if (message.Length > 100)
                {
                    message = message.Substring(0, 100);
                }

                TextMessagePacket packet = new(_nickname, message);
                _writer.Reset();

                if (_isServer)
                {
                    Singleton<FikaServer>.Instance.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }
                else
                {
                    Singleton<FikaClient>.Instance.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }
                AudioClip outgoingClip = _soundsWrapper.GetSocialNetworkClip(ESocialNetworkSoundType.OutgoingMessage);
                Singleton<GUISounds>.Instance.PlaySound(outgoingClip);
                AddMessage(_nickname + ": " + message);
            }
        }

        public void ReceiveMessage(string nickname, string message)
        {
            AudioClip incomingClip = _soundsWrapper.GetSocialNetworkClip(ESocialNetworkSoundType.IncomingMessage);
            Singleton<GUISounds>.Instance.PlaySound(incomingClip);
            AddMessage(nickname + ": " + message);
        }

        private void AddMessage(string message)
        {
            _chatText = string.Concat(_chatText, message, "\n");
            if (_chatText.Length > 1000)
            {
                _chatText = _chatText.Substring(500);
            }
        }

        private void DrawWindow(int windowId)
        {
            Rect rect = new(5, 15, 580, 200);
            GUI.Label(rect, _chatText);
            rect.y += rect.height;
            Rect textRect = new(rect.x, rect.y, rect.width - 55, 25);
            GUI.SetNextControlName(_textFieldName);
            _textField = GUI.TextField(textRect, _textField);
            if (_selectText)
            {
                GUI.FocusControl(_textFieldName);
                _selectText = false;
            }
            rect.x += 535;
            Rect buttonRect = new(rect.x, rect.y, 50, 25);
            if (GUI.Button(buttonRect, "SEND"))
            {
                SendMessage();
                GUI.UnfocusWindow();
                GUI.FocusControl(null);
            }
        }
    }
}
