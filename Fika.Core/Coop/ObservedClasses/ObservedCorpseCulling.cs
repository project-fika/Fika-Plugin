using EFT.Interactive;
using Fika.Core.Coop.Players;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
    public class ObservedCorpseCulling : IDisposable
    {
        public bool IsVisible;

        private readonly ObservedCoopPlayer _observedCoopPlayer;
        private readonly Corpse _observedCorpse;
        private readonly List<Renderer> _renderers = new(256);
        private GClass997 _gClass997;
        private bool _ragdollDone;
        private bool _localVisible = true;

        public ObservedCorpseCulling(ObservedCoopPlayer observedCoopPlayer, Corpse observedCorpse)
        {
            _observedCoopPlayer = observedCoopPlayer;
            _observedCorpse = observedCorpse;
            _gClass997 = new(observedCoopPlayer.PlayerBones.Spine3.Original,
                EFTHardSettings.Instance.CULLING_PLAYER_SPHERE_DEAD_BODY_RADIUS,
                EFTHardSettings.Instance.CULLING_PLAYER_DEAD_BODY_DISTANCE);
            _gClass997.OnVisibilityChanged += ChangeVisibility;
            _gClass997.Register();
        }

        public void ManualUpdate()
        {
            if (!_ragdollDone)
            {
                _gClass997.CustomUpdate();
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
            _observedCoopPlayer.PlayerBody.GetRenderersNonAlloc(_renderers);
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
            _gClass997.OnVisibilityChanged -= ChangeVisibility;
            _gClass997?.Dispose();
            _gClass997 = null;
            IsVisible = false;
            ChangeRendererState();
        }
    }
}
