using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using HarmonyLib;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fika.Core.UI.Custom
{
    public class FikaChatUIScript : UIInputNode
    {
        public static bool IsActive { get; internal set; }

        public List<ChatMessage> ChatMessages
        {
            get
            {
                return _chatMessages;
            }
        }

        private FikaChatUI _fikaChatUI;
        private List<ChatMessage> _chatMessages;
        private StringBuilder _stringBuilder;
        private string _nickname;
        private bool _isServer;
        private bool _shouldClose;
        private float _closeTimer;
        private float _closeThreshold;
        private bool _isActive;
        private GameObject _mainScreen;

        private void Update()
        {
            if (_shouldClose)
            {
                _closeTimer += Time.unscaledDeltaTime;
                if (_closeTimer >= _closeThreshold)
                {
                    _closeTimer = 0f;
                    CloseChat();
                }
            }
        }

        private void Awake()
        {
            _fikaChatUI = GetComponent<FikaChatUI>();
            if (_fikaChatUI == null)
            {
                throw new NullReferenceException("Could not find FikaChatUI");
            }

            _chatMessages = [];
            _stringBuilder = new();
            _nickname = FikaBackendUtils.PMCName ?? "Unknown";
            _isServer = FikaBackendUtils.IsServer;
            _closeTimer = 0f;
            _closeThreshold = 3f;
            _mainScreen = gameObject.transform.GetChild(0).gameObject;

            _fikaChatUI.InputField.onSubmit.AddListener(OnSubmit);
            _fikaChatUI.SendButton.onClick.AddListener(SendMessage);
            _fikaChatUI.CloseButton.onClick.AddListener(CloseChat);
        }

        private void Start()
        {
            FikaGlobals.InputTree.Add(this);

            _isActive = false;
            _shouldClose = false;
            _mainScreen.SetActive(false);
        }

        public void AddMessage(ChatMessage message, bool remote = false)
        {
            _chatMessages.Add(message);
            if (_stringBuilder.Length > 10000) // to avoid too much data
            {
                _stringBuilder.Clear();
            }
            _stringBuilder.AppendLine(message.FormattedMessage);

            _fikaChatUI.ChatText.SetText(_stringBuilder.ToString());
            _fikaChatUI.ScrollView.verticalNormalizedPosition = 0f; // scroll to bottom

            if (remote)
            {
                Singleton<GUISounds>.Instance.PlayChatSound(ESocialNetworkSoundType.IncomingMessage);
            }
        }

        private void OnSubmit(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            ChatMessage chatMessage = new(_nickname, message);
            AddMessage(chatMessage);

            TextMessagePacket packet = new(_nickname, message);
            if (_isServer)
            {
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
            Singleton<GUISounds>.Instance.PlayChatSound(ESocialNetworkSoundType.OutgoingMessage);
            _fikaChatUI.InputField.DeactivateInputField();
            _fikaChatUI.InputField.text = string.Empty;
        }

        public void OpenChat(bool autoClose = false)
        {
            _mainScreen.SetActive(true);
            _closeTimer = 0f;

            bool shouldAutoClose = autoClose && !_isActive;
            if (!_isActive && !autoClose)
            {
                _isActive = true;
                IsActive = true;
            }
            _shouldClose = shouldAutoClose;
        }

        public void CloseChat()
        {
            if (_isActive)
            {
                UIEventSystem.Instance.SetTemporaryStatus(false);
            }
            _isActive = false;
            IsActive = false;
            _shouldClose = false;
            _mainScreen.SetActive(false);

            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuEscape);
        }

        public void OpenAndSelectInput()
        {
            OpenChat();
            SelectInput();
        }

        public void ToggleChat()
        {
            bool isActive = _mainScreen.activeSelf;
            if (!isActive)
            {
                OpenAndSelectInput();
            }
            else
            {
                if (_shouldClose) // this means it was showing from a message, and the player wants to interact with the window
                {
                    OpenAndSelectInput();
                    return;
                }

                CloseChat();
            }
        }

        private void SendMessage()
        {
            _fikaChatUI.InputField.onSubmit.Invoke(_fikaChatUI.InputField.text);
        }

        private void SelectInput()
        {
            _fikaChatUI.InputField.ActivateInputField();
            UIEventSystem.Instance.SetTemporaryStatus(true);
            EventSystem.current.SetSelectedGameObject(_mainScreen);
        }

        private void OnDestroy()
        {
            FikaGlobals.InputTree.Remove(this);
            _stringBuilder.Clear();
            Destroy(_fikaChatUI.gameObject);
        }

        internal static FikaChatUIScript Create()
        {
            GameObject gameObject = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.FikaChatUI);
            GameObject obj = Instantiate(gameObject);
            FikaChatUIScript uiScript = obj.AddComponent<FikaChatUIScript>();
            RectTransform rectTransform = obj.transform.GetChild(0).GetChild(0).RectTransform();
            if (rectTransform == null)
            {
                FikaGlobals.LogError("Could not get the RectTransform!");
                Destroy(obj);
                return null;
            }
            rectTransform.gameObject.AddComponent<UIDragComponent>().Init(rectTransform, true);
            obj.SetActive(true);

            return uiScript;
        }

        public override ETranslateResult TranslateCommand(ECommand command)
        {
            if (!_isActive)
            {
                return ETranslateResult.Ignore;
            }

            if (command.IsCommand(ECommand.Escape))
            {
                CloseChat();
                return ETranslateResult.Block;
            }

            if (command.IsCommand(ECommand.Enter))
            {
                SelectInput();
                return ETranslateResult.Block;
            }

            return ETranslateResult.Block;
        }

        public override void TranslateAxes(ref float[] axes)
        {
            if (_isActive)
            {
                axes = null;
            }
        }

        public override ECursorResult ShouldLockCursor()
        {
            if (!_isActive)
            {
                return ECursorResult.Ignore;
            }

            return ECursorResult.ShowCursor;
        }
    }

    public record ChatMessage
    {
        private readonly string _sender;
        private readonly string _message;

        public ChatMessage(string sender, string message)
        {
            _sender = sender;
            _message = message;
        }

        public string FormattedMessage
        {
            get
            {
                return $"[{_sender}]: {_message}";
            }
        }
    }
}
