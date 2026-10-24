using System.Collections.Generic;
using CommonAssets.Scripts.Cutscenes;
using Comfort.Common;
using EFT;

namespace Fika.Core.Main.Patches.Cutscenes;

internal static class ObservedPlayersAside
{
    private readonly record struct SetAsidePlayer(int Index, Player Player);

    private static readonly List<SetAsidePlayer> _setAside = [];
    private static int _depth;
    private static CutscenesClientController _viewsEnabledWhileAside;

    private static bool IsAside
    {
        get
        {
            return _depth > 0;
        }
    }

    public static void Begin()
    {
        _depth++;
        
        if (_depth > 1)
        {
            return;
        }

        var players = Singleton<GameWorld>.Instance.AllAlivePlayersList;
        for (var i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].ActiveHealthController == null)
            {
                _setAside.Add(new SetAsidePlayer(i, players[i]));
                players.RemoveAt(i);
            }
        }
    }

    public static void End()
    {
        _depth--;

        if (IsAside)
        {
            return;
        }

        var players = Singleton<GameWorld>.Instance.AllAlivePlayersList;
        for (var i = _setAside.Count - 1; i >= 0; i--)
        {
            players.Insert(_setAside[i].Index, _setAside[i].Player);
        }

        _setAside.Clear();

        var controller = _viewsEnabledWhileAside;
        _viewsEnabledWhileAside = null;
        controller?.EnableNoCutsceneObjectsViews();
    }

    public static void ViewsEnabled(CutscenesClientController controller)
    {
        if (IsAside)
        {
            _viewsEnabledWhileAside = controller;
        }
    }
}
