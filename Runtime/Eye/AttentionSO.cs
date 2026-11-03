using System;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>Something worth a look for a moment (<see cref="AttentionSO"/>).</summary>
  public readonly struct AttentionCue
  {
    /// <summary>What to look at (a character's head, a pot, a door).</summary>
    public readonly Transform Source;

    /// <summary>Who raised it, so they never look at their own cue (null for no one).</summary>
    public readonly GameObject Owner;

    /// <summary>How much it draws the eye, 0 to 1 (a shout 1, a sigh 0.3).</summary>
    public readonly float Strength;

    /// <summary>When it stops drawing the eye, in game time.</summary>
    public readonly float Until;

    public AttentionCue(Transform source, GameObject owner, float strength, float until)
    {
      Source = source;
      Owner = owner;
      Strength = strength;
      Until = until;
    }
  }

  /// <summary>
  /// Who acts, for everyone around to look at. One asset is the game's attention: whatever
  /// shows someone acting raises a cue on it (a character reacts, speaks; a door slams), and
  /// every <see cref="Gaze"/> that holds the asset may glance at the strongest, nearest cue.
  /// Raisers never know who watches. A system can mute it while it runs (a fight: eyes stay
  /// on the fight); a muted attention takes no cues and offers none. Game time: a paused game
  /// holds its cues. The cues are runtime state, never saved with the asset.
  /// </summary>
  [CreateAssetMenu(fileName = "Attention", menuName = "CupkekGames/Character/Attention")]
  public class AttentionSO : ScriptableObject
  {
    [NonSerialized] private readonly List<AttentionCue> _cues = new();
    [NonSerialized] private readonly List<UnityEngine.Object> _mutedBy = new();

    /// <summary>A cue was raised.</summary>
    public event Action<AttentionCue> Raised;

    /// <summary>Whether something muted it (cues are dropped while it is).</summary>
    public bool IsMuted
    {
      get
      {
        _mutedBy.RemoveAll(by => by == null);
        return _mutedBy.Count > 0;
      }
    }

    /// <summary>The cues still drawing the eye.</summary>
    public IReadOnlyList<AttentionCue> Cues
    {
      get
      {
        Prune();
        return _cues;
      }
    }

    /// <summary>
    /// <paramref name="source"/> draws the eye for <paramref name="seconds"/>, at
    /// <paramref name="strength"/> (0 to 1). <paramref name="owner"/> (who acts) never looks
    /// at its own cue. Nothing while muted.
    /// </summary>
    public void Raise(Transform source, float strength = 1f, float seconds = 1.5f, GameObject owner = null)
    {
      if (source == null) throw new ArgumentNullException(nameof(source));
      if (IsMuted) return;
      Prune();
      var cue = new AttentionCue(source, owner, Mathf.Clamp01(strength), Time.time + Mathf.Max(0.05f, seconds));
      _cues.Add(cue);
      Raised?.Invoke(cue);
    }

    /// <summary>
    /// The cue that draws the eye most at <paramref name="from"/> within <paramref name="radius"/>:
    /// strength fading with distance, never <paramref name="self"/>'s own or one on itself.
    /// </summary>
    public bool TryBest(Vector3 from, GameObject self, float radius, out AttentionCue best)
    {
      best = default;
      if (IsMuted) return false;
      Prune();
      float bestScore = 0f;
      foreach (AttentionCue cue in _cues)
      {
        if (cue.Owner != null && cue.Owner == self) continue;
        if (self != null && cue.Source.IsChildOf(self.transform)) continue;
        if (!cue.Source.gameObject.activeInHierarchy) continue;

        float distance = Vector3.Distance(from, cue.Source.position);
        if (distance > radius) continue;
        float score = cue.Strength * (1f - distance / Mathf.Max(radius, 1e-3f));
        if (score <= bestScore) continue;
        bestScore = score;
        best = cue;
      }

      return bestScore > 0f;
    }

    /// <summary>Mutes it until <paramref name="by"/> unmutes it (or is destroyed); drops the cues there are.</summary>
    public void Mute(UnityEngine.Object by)
    {
      if (by == null) throw new ArgumentNullException(nameof(by));
      if (!_mutedBy.Contains(by)) _mutedBy.Add(by);
      _cues.Clear();
    }

    /// <summary>Lifts <paramref name="by"/>'s mute.</summary>
    public void Unmute(UnityEngine.Object by) => _mutedBy.Remove(by);

    // Cleared when the asset unloads. Between play sessions without a domain reload, the cues
    // and mutes of the last session drop on their own: their objects are gone.
    private void OnDisable()
    {
      _cues.Clear();
      _mutedBy.Clear();
    }

    private void Prune()
    {
      float now = Time.time;
      _cues.RemoveAll(cue => cue.Source == null || cue.Until <= now);
    }
  }
}
