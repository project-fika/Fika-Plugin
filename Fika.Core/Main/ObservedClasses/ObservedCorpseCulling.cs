using EFT.Interactive;
using Fika.Core.Main.Players;
using System;
using System.Collections.Generic;

namespace Fika.Core.Main.ObservedClasses;

public class ObservedCorpseCulling : IDisposable
{
    public bool IsVisible;

    private readonly ObservedPlayer _observedPlayer;
    private readonly Corpse _observedCorpse;
    private readonly List<Renderer> _renderers = new(256);
    private GClass999 _gClass999;
    private bool _ragdollDone;
    private bool _localVisible = true;

    public ObservedCorpseCulling(ObservedPlayer observedPlayer, Corpse observedCorpse)
    {
        _observedPlayer = observedPlayer;
        _observedCorpse = observedCorpse;
        _gClass999 = new(observedPlayer.PlayerBones.Spine3.Original,
            EFTHardSettings.Instance.CULLING_PLAYER_SPHERE_DEAD_BODY_RADIUS,
            EFTHardSettings.Instance.CULLING_PLAYER_DEAD_BODY_DISTANCE);
        _gClass999.OnVisibilityChanged += ChangeVisibility;
        _gClass999.Register();
    }

    public void ManualUpdate()
    {
        if (!_ragdollDone)
        {
            _gClass999.CustomUpdate();
        }
        if (!_ragdollDone && _observedCorpse.Ragdoll.Bool_2)
        {
            _ragdollDone = true;
        }
        if (_localVisible != IsVisible)
        {
            IsVisible = _localVisible;
            ChangeRendererState();
        }
    }

    private void ChangeVisibility(bool value)
    {
        _localVisible = value;
    }

    private void ChangeRendererState()
    {
        for (int i = 0; i < _renderers.Count; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].forceRenderingOff = false;
            }
        }
        _renderers.Clear();
        bool isVisible = IsVisible;
        _observedPlayer.PlayerBody.GetRenderersNonAlloc(_renderers);
        for (int k = 0; k < _renderers.Count; k++)
        {
            if (_renderers[k] != null)
            {
                _renderers[k].forceRenderingOff = !isVisible;
            }
        }
    }

    public void Dispose()
    {
        _gClass999.OnVisibilityChanged -= ChangeVisibility;
        _gClass999?.Dispose();
        _gClass999 = null;
        IsVisible = false;
        ChangeRendererState();
    }
}
