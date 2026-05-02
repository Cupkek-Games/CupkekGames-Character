using System;
using System.Threading;
using CupkekGames.TimeSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

using CupkekGames.VFX;

namespace CupkekGames.Character
{
  /// <summary>
  /// Controls emote particle playback, ensuring only one particle is active at a time.
  /// Cancels previous particles when a new one is requested.
  /// </summary>
  public class EmoteParticleController : IDisposable
  {
    private CancellationTokenSource _emoteParticleCts;
    private GameObject _activeEmoteParticle;
    private TimeBundle _activeEmoteTimeBundle;

    /// <summary>
    /// Plays an emote particle effect. If a particle is already playing, it will be cancelled first.
    /// </summary>
    /// <param name="vfx">The VFX bundle to play</param>
    /// <param name="parent">The parent GameObject for the particle</param>
    /// <param name="targetTransform">The transform to position the particle at</param>
    /// <param name="durationMs">Duration in milliseconds before the particle is destroyed</param>
    /// <param name="timeManager">The TimeManager instance to create TimeBundle</param>
    public async UniTask PlayParticle(VFXBundle vfx, GameObject parent, Transform targetTransform, int durationMs,
      TimeManager timeManager)
    {
      if (vfx == null)
      {
        return;
      }

      // Cancel and cleanup previous particle if exists
      CancelPreviousParticle();

      // Create new cancellation token source for this particle
      _emoteParticleCts = new CancellationTokenSource();
      CancellationToken ct = _emoteParticleCts.Token;

      // Create new TimeBundle and store reference
      _activeEmoteTimeBundle = new TimeBundle(timeManager);

      // Store local references for cleanup (to avoid cleaning up if replaced by new call)
      CancellationTokenSource localCts = _emoteParticleCts;
      TimeBundle localTimeBundle = _activeEmoteTimeBundle;

      vfx.DestroyDelayMS = durationMs;
      try
      {
        _activeEmoteParticle = await vfx.Play(parent, targetTransform, ct, localTimeBundle, null);
      }
      catch (OperationCanceledException)
      {
        // Particle was cancelled - VFXBundle.Play() should have deactivated the GameObject,
        // but ensure cleanup just in case (e.g., if cancellation happened before GameObject creation)
        if (_activeEmoteParticle != null && _activeEmoteParticle.activeSelf)
        {
          _activeEmoteParticle.SetActive(false);
        }

        _activeEmoteParticle = null;
        return;
      }
      finally
      {
        // Only cleanup if these are still the active references (not replaced by new call)
        if (_emoteParticleCts == localCts && _activeEmoteTimeBundle == localTimeBundle)
        {
          _activeEmoteParticle = null;
          _activeEmoteTimeBundle.Dispose();
          _activeEmoteTimeBundle = null;
          _emoteParticleCts.Dispose();
          _emoteParticleCts = null;
        }
      }
    }

    /// <summary>
    /// Cancels and cleans up the currently active particle.
    /// </summary>
    public void CancelPreviousParticle()
    {
      // Store reference before cancelling
      GameObject particleToDeactivate = _activeEmoteParticle;

      // Cancel the token first to trigger cancellation in VFXBundle.Play()
      if (_emoteParticleCts != null)
      {
        _emoteParticleCts.Cancel();
        _emoteParticleCts.Dispose();
        _emoteParticleCts = null;
      }

      // Deactivate the particle GameObject if we have a reference
      // This handles the case where we already have the reference from a previous call
      if (particleToDeactivate != null && particleToDeactivate.activeSelf)
      {
        particleToDeactivate.SetActive(false);
      }

      _activeEmoteParticle = null;

      // Dispose TimeBundle after ensuring GameObject is deactivated
      if (_activeEmoteTimeBundle != null)
      {
        _activeEmoteTimeBundle.Dispose();
        _activeEmoteTimeBundle = null;
      }
    }

    public void Dispose()
    {
      CancelPreviousParticle();
    }
  }
}
