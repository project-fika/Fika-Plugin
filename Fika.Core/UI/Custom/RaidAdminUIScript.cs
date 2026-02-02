using System.Collections;
using System.Collections.Generic;
using EFT.InputSystem;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using TMPro;

namespace Fika.Core.UI.Custom;

public class RaidAdminUIScript : InputNode
{
    private RaidAdminUI _raidAdminUI;
    private FikaServer _server;
    private NetManager _netManager;
    private LiteNetPeer _currentPeer;
    private float _counter;
    private float _counterThreshold;
    private List<LiteNetPeer> _peers;

    private void Awake()
    {
        _raidAdminUI = gameObject.GetComponent<RaidAdminUI>();

        _counter = 0f;
        _counterThreshold = 1f;
        _peers = [];

        _raidAdminUI.ClientSelection.onValueChanged.AddListener(OnClientSelection);
        _raidAdminUI.KickButton.onClick.AddListener(OnKickButton);
        _raidAdminUI.CloseButton.onClick.AddListener(Close);

        FikaGlobals.InputTree.Add(this);

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_currentPeer != null)
        {
            _counter += Time.unscaledDeltaTime;
            if (_counter >= _counterThreshold)
            {
                _counter -= _counterThreshold;
                UpdatePeerData();
            }
        }
    }

    private void OnDestroy()
    {
        FikaGlobals.InputTree.Remove(this);
    }

    private void UpdatePeerData()
    {
        var statistics = _currentPeer.Statistics;

        _raidAdminUI.SentDataText.SetText($"Sent Data: {FormatBytes(statistics.BytesSent)}");
        _raidAdminUI.ReceivedDataText.SetText($"Received Data: {FormatBytes(statistics.BytesReceived)}");
        _raidAdminUI.SentPacketsText.SetText("Sent Packets: {0}", statistics.PacketsSent);
        _raidAdminUI.ReceivedPacketsText.SetText("Received Packets: {0}", statistics.PacketsReceived);
        _raidAdminUI.PacketLossText.SetText("Packet Loss: {0}", statistics.PacketLoss);
        _raidAdminUI.PacketLossPercentText.SetText("Packet Loss %: {0}%", statistics.PacketLossPercent);
    }

    private string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }
        else if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024f:F2} KB";
        }
        else if (bytes < 1024 * 1024 * 1024)
        {
            return $"{bytes / 1024f / 1024f:F2} MB";
        }
        else
        {
            return $"{bytes / 1024f / 1024f / 1024f:F2} GB";
        }
    }

    private void OnKickButton()
    {
        if (_currentPeer != null)
        {
            _netManager.DisconnectPeer(_currentPeer);
            RefreshOptions();
        }
    }

    private void OnClientSelection(int index)
    {
        if (index < 0)
        {
            _currentPeer = null;
            return;
        }

        _netManager.GetConnectedPeers(_peers);
        if (_peers[index] != null)
        {
            _currentPeer = _peers[index];
            _raidAdminUI.InfoPane.SetActive(true);
            _raidAdminUI.HeaderText.SetText("Client {0}", index);
        }
        else
        {
            _currentPeer = null;
        }
    }

    private void ResetInfoPane()
    {
        _raidAdminUI.HeaderText.SetText(string.Empty);
        _raidAdminUI.SentDataText.SetText(string.Empty);
        _raidAdminUI.ReceivedDataText.SetText(string.Empty);
        _raidAdminUI.SentPacketsText.SetText(string.Empty);
        _raidAdminUI.ReceivedPacketsText.SetText(string.Empty);
        _raidAdminUI.PacketLossText.SetText(string.Empty);
        _raidAdminUI.PacketLossPercentText.SetText(string.Empty);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        // used to bypass the console screen close coroutine
        WaitForEndOfFrame waitForEndOfFrame = new();
        yield return waitForEndOfFrame;
        yield return waitForEndOfFrame;

        RefreshOptions();

        UIEventSystem.Instance.SetTemporaryStatus(true);
    }

    private void RefreshOptions()
    {
        ResetInfoPane();
        _raidAdminUI.InfoPane.SetActive(false);
        var clientSelection = _raidAdminUI.ClientSelection;
        clientSelection.ClearOptions();
        List<TMP_Dropdown.OptionData> options = [];

        _netManager.GetConnectedPeers(_peers);
        for (var i = 0; i < _peers.Count; i++)
        {
            options.Add(new($"Client {i}"));
        }

        if (options.Count < 1)
        {
            options.Add(new("No clients"));
            clientSelection.interactable = false;
        }
        else
        {
            clientSelection.interactable = true;
            clientSelection.onValueChanged.Invoke(0);
        }

        clientSelection.AddOptions(options);
    }

    public void Close()
    {
        UIEventSystem.Instance.SetTemporaryStatus(false);
        _currentPeer = null;
        gameObject.SetActive(false);
    }

    public static RaidAdminUIScript Create(FikaServer server, NetManager manager)
    {
        var gameObject = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.RaidAdminUI);
        var obj = Instantiate(gameObject);
        var uiScript = obj.AddComponent<RaidAdminUIScript>();

        var rectTransform = obj.transform.GetChild(0).GetChild(0).RectTransform();
        if (rectTransform == null)
        {
            FikaGlobals.LogError("Could not get the RectTransform!");
            Destroy(obj);
            return null;
        }
        rectTransform.gameObject.AddComponent<UIDragComponent>().Init(rectTransform, true);

        uiScript._server = server;
        uiScript._netManager = manager;

        DontDestroyOnLoad(obj);

        return uiScript;
    }

    public override ECursorResult ShouldLockCursor()
    {
        return ECursorResult.ShowCursor;
    }

    public override void TranslateAxes(ref float[] axes)
    {
        axes = null;
    }

    public override ETranslateResult TranslateCommand(ECommand command)
    {
        if (command.IsCommand(ECommand.Escape))
        {
            Close();
            return ETranslateResult.BlockAll;
        }

        return GetDefaultBlockResult(command);
    }

    internal void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Close();
        }
        else
        {
            Show();
        }
    }
}
