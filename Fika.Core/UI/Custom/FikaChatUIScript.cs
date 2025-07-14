using Comfort.Common;
using EFT.InputSystem;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fika.Core.UI.Custom
{
    public class FikaChatUIScript : MonoBehaviour
    {
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
        private UISoundsWrapper _soundsWrapper;
        private InputManager _manager;
        private string _nickname;
        private bool _isServer;

        public void AddMessage(ChatMessage message)
        {
            _chatMessages.Add(message);
            _stringBuilder.AppendLine(message.FormattedMessage);

            _fikaChatUI.ChatText.SetText(_stringBuilder.ToString());

            _fikaChatUI.ScrollView.verticalNormalizedPosition = 0f; // scroll to bottom
        }

        private void Awake()
        {
            _fikaChatUI = GetComponent<FikaChatUI>();
            if (_fikaChatUI == null)
            {
                throw new NullReferenceException("Could not find FikaChatUI");
            }

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
            }

            _chatMessages = [];
            _stringBuilder = new();
            _nickname = FikaBackendUtils.PMCName ?? "Unknown";
            _isServer = FikaBackendUtils.IsServer;

            _fikaChatUI.InputField.onSubmit.AddListener(OnSubmit);
            _fikaChatUI.SendButton.onClick.AddListener(SendMessage);
            _fikaChatUI.CloseButton.onClick.AddListener(CloseChat);
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
            /*if (_isServer)
            {
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Singleton<FikaClient>.Instance.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }*/
            AudioClip outgoingClip = _soundsWrapper.GetSocialNetworkClip(ESocialNetworkSoundType.OutgoingMessage);
            Singleton<GUISounds>.Instance.PlaySound(outgoingClip);
            _fikaChatUI.InputField.DeactivateInputField();
            _fikaChatUI.InputField.text = string.Empty;
        }

        public void OpenChat()
        {
            gameObject.SetActive(true);
        }

        public void CloseChat()
        {
            gameObject.SetActive(false);
        }

        private void SendMessage()
        {
            _fikaChatUI.InputField.onSubmit.Invoke(_fikaChatUI.InputField.text);
        }

        private void OnDestroy()
        {
            _stringBuilder.Clear();
            Destroy(_fikaChatUI.gameObject);
        }

        internal static void Create()
        {
            GameObject gameObject = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.FikaChatUI);
            GameObject obj = Instantiate(gameObject);
            obj.AddComponent<FikaChatUIScript>();
            RectTransform rectTransform = obj.transform.GetChild(0).GetChild(0).RectTransform();
            if (rectTransform == null)
            {
                FikaGlobals.LogError("Could not get the RectTransform!");
                Destroy(obj);
                return;
            }
            rectTransform.gameObject.AddComponent<UIDragComponent>().Init(rectTransform, true);
            obj.SetActive(true);
        }
    }

    public class ChatMessage(string sender, string message)
    {
        private readonly string _sender = sender;
        private readonly string _message = message;

        public string FormattedMessage
        {
            get
            {
                return $"[{_sender}]: {_message}";
            }
        }
    }
}
