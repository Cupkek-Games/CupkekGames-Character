using System;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// Composes a character's face each frame and writes every channel once. Layers, bottom up:
  /// <list type="number">
  /// <item>the expression layer: a crossfade from a snapshot of the current face to a target
  /// <see cref="FaceExpressionSO"/> (absent channels = 0), returning to <see cref="Rest"/> when a
  /// timed <see cref="Play(FaceExpressionSO, float)"/> ends;</item>
  /// <item>the blink: <see cref="Blink01"/> closes the <see cref="BlinkChannels"/>, gated by the
  /// expressions' <see cref="FaceBlinkPolicy"/> (the gate crossfades with the face);</item>
  /// <item>mid-path corrections: each written channel drives its <c>X_mid</c> shape.</item>
  /// </list>
  /// The rig binds every child mesh with shape keys (minus <see cref="_exclude"/>). Channels no
  /// expression or blink ever touched are never written, so body shape keys keep their values.
  /// </summary>
  [DisallowMultipleComponent]
  public class FaceController : MonoBehaviour
  {
    public static readonly string[] DefaultBlinkChannels = { "eyeBlinkLeft", "eyeBlinkRight" };

    [Tooltip("The face this character returns to after a timed expression.")]
    [SerializeField] private FaceExpressionSO _rest;

    [Tooltip("Channels the blink closes (to 100).")]
    [SerializeField] private List<string> _blinkChannels = new(DefaultBlinkChannels);

    [Tooltip("Child meshes with shape keys that are not part of the face.")]
    [SerializeField] private List<SkinnedMeshRenderer> _exclude = new();

    public event Action<FaceExpressionSO> ExpressionChanged;

    private FaceRig _rig;

    // Expression layer, keyed by channel name so a rig rebuild keeps the state.
    private readonly Dictionary<string, float> _from = new();
    private readonly Dictionary<string, float> _to = new();
    private readonly Dictionary<string, float> _value = new();
    private readonly List<string> _touched = new();
    private readonly HashSet<string> _touchedSet = new();
    private readonly HashSet<string> _blinkSet = new();

    private FaceExpressionSO _target;
    private bool _hasTarget;
    private float _fadeStart;
    private float _fadeSeconds;
    private float _fromGate = 1f;
    private float _toGate = 1f;
    private float _gate = 1f;
    private float _holdUntil = -1f;
    private float _blink01;

    public FaceRig Rig
    {
      get
      {
        EnsureRig();
        return _rig;
      }
    }

    public FaceExpressionSO Rest => _rest;

    /// <summary>The expression the face is showing or fading to.</summary>
    public FaceExpressionSO Current => _target;

    public IReadOnlyList<string> BlinkChannels => _blinkChannels;

    /// <summary>How closed the blink holds the eyes, 0..1. Written by <see cref="Blinker"/>.</summary>
    public float Blink01
    {
      get => _blink01;
      set => _blink01 = Mathf.Clamp01(value);
    }

    /// <summary>Whether blinking currently shows (1) or is suppressed by the expression (0).</summary>
    public float BlinkGate => _gate;

    public bool HasChannel(string name) => Rig.HasChannel(name);

    /// <summary>Every child mesh with shape keys, minus the exclude list: what the rig binds.</summary>
    public List<SkinnedMeshRenderer> CollectRenderers()
    {
      var renderers = new List<SkinnedMeshRenderer>();
      foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
      {
        if (renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0) continue;
        if (_exclude.Contains(renderer)) continue;
        renderers.Add(renderer);
      }

      return renderers;
    }

    /// <summary>Rebind after meshes were added, removed or swapped under this character.</summary>
    public void RebuildRig()
    {
      _rig = FaceRig.Build(CollectRenderers());
      _blinkSet.Clear();
      foreach (string channel in _blinkChannels)
      {
        if (string.IsNullOrEmpty(channel)) continue;
        _blinkSet.Add(channel);
        Touch(channel);
      }
    }

    /// <summary>Show <paramref name="expression"/> with its own blend time.</summary>
    /// <param name="holdSeconds">Seconds from now until the face returns to rest; 0 or less holds until the next call.</param>
    public void Play(FaceExpressionSO expression, float holdSeconds = 0f)
    {
      if (expression == null) throw new ArgumentNullException(nameof(expression));
      Play(expression, holdSeconds, expression.BlendSeconds);
    }

    public void Play(FaceExpressionSO expression, float holdSeconds, float blendSeconds)
    {
      if (expression == null) throw new ArgumentNullException(nameof(expression));
      PlayAt(expression, holdSeconds, blendSeconds, Time.time);
    }

    /// <summary>Jump straight to <paramref name="expression"/> and write it now (works in edit mode).</summary>
    public void Snap(FaceExpressionSO expression)
    {
      if (expression == null) throw new ArgumentNullException(nameof(expression));
      PlayAt(expression, 0f, 0f, Time.time);
      ApplyNow();
    }

    /// <summary>Return to rest now.</summary>
    public void Clear() => Clear(_rest != null ? _rest.BlendSeconds : 0f);

    public void Clear(float blendSeconds)
    {
      _holdUntil = -1f;
      BeginFade(_rest, blendSeconds, Time.time);
    }

    /// <summary>
    /// Change the face this character rests on (a mood face, say). If it is resting now,
    /// it fades to the new rest; a timed expression in progress returns to it when it ends.
    /// </summary>
    public void SetRest(FaceExpressionSO rest)
    {
      bool resting = _holdUntil < 0f && _target == _rest;
      _rest = rest;
      if (resting) BeginFade(_rest, _rest != null ? _rest.BlendSeconds : 0f, Time.time);
    }

    /// <summary>Finish the current fade and write the face now. For edit-mode preview and Timeline scrubbing.</summary>
    public void ApplyNow()
    {
      EnsureRig();
      _rig.Invalidate();
      _fadeSeconds = 0f;
      Compose(1f);
    }

    internal void PlayAt(FaceExpressionSO expression, float holdSeconds, float blendSeconds, float now)
    {
      BeginFade(expression, blendSeconds, now);
      _holdUntil = holdSeconds > 0f ? now + holdSeconds : -1f;
    }

    internal void Evaluate(float now)
    {
      EnsureRig();

      if (_holdUntil >= 0f && now >= _holdUntil)
      {
        float start = _holdUntil;
        _holdUntil = -1f;
        BeginFade(_rest, _rest != null ? _rest.BlendSeconds : 0f, start);
      }

      float t = _fadeSeconds <= 0f ? 1f : Mathf.Clamp01((now - _fadeStart) / _fadeSeconds);
      Compose(t * t * (3f - 2f * t));
    }

    private void OnEnable()
    {
      EnsureRig();
      if (!_hasTarget && _rest != null) BeginFade(_rest, 0f, Time.time);
    }

    private void LateUpdate()
    {
      Evaluate(Time.time);
    }

    private void EnsureRig()
    {
      if (_rig == null) RebuildRig();
    }

    private void BeginFade(FaceExpressionSO expression, float seconds, float start)
    {
      // Snapshot the face as it stands, so a new fade starts where the last one got to.
      _from.Clear();
      foreach (KeyValuePair<string, float> pair in _value) _from[pair.Key] = pair.Value;
      _fromGate = _gate;

      _to.Clear();
      if (expression != null)
      {
        foreach (FaceWeight weight in expression.Weights)
        {
          if (string.IsNullOrEmpty(weight.Name)) continue;
          _to[weight.Name] = weight.Weight;
          Touch(weight.Name);
        }
      }

      _toGate = expression == null || expression.Blink == FaceBlinkPolicy.Allow ? 1f : 0f;
      _target = expression;
      _hasTarget = true;
      _fadeStart = start;
      _fadeSeconds = Mathf.Max(0f, seconds);

      ExpressionChanged?.Invoke(expression);
    }

    private void Compose(float s)
    {
      _gate = Mathf.Lerp(_fromGate, _toGate, s);
      float blink = _blink01 * _gate;

      for (int i = 0; i < _touched.Count; i++)
      {
        string name = _touched[i];
        _from.TryGetValue(name, out float from);
        _to.TryGetValue(name, out float to);
        float value = Mathf.Lerp(from, to, s);
        _value[name] = value;

        if (!_rig.TryGetChannel(name, out FaceRig.Channel channel)) continue;

        float final = blink > 0f && _blinkSet.Contains(name) ? Mathf.Lerp(value, 100f, blink) : value;
        _rig.Write(channel, final);
      }
    }

    private void Touch(string name)
    {
      if (_touchedSet.Add(name)) _touched.Add(name);
    }
  }
}
