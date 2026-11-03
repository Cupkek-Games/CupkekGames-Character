using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// Where a character looks, and the life in her eyes. It moves the point her eyes aim at
  /// (<see cref="EyeAim"/>'s target) each frame, on three levels, the first that applies:
  /// <list type="number">
  /// <item>a look someone asked for (<see cref="LookAt(Transform, float, float)"/>,
  /// <see cref="LookAtCamera"/>) until it ends or <see cref="Release"/>;</item>
  /// <item>who acts nearby (<see cref="AttentionSO"/>): she glances at the strongest cue after a
  /// moment's delay, her head turning part way, and looks back when it ends;</item>
  /// <item>her own look: quick small glances around it and now and then a bigger look around.</item>
  /// </list>
  /// The eyes jump in saccades (fast, then still) rather than gliding, and track a moving
  /// target smoothly; a jump past <see cref="_blinkOverDegrees"/> blinks, as people do. A
  /// followed target that goes away (destroyed, hidden) ends the look. The head turns through
  /// <see cref="HeadLook"/> when the character has one, and only while a look asks for it.
  /// Put it beside the <see cref="HumonoidCharacter"/>.
  /// </summary>
  [DisallowMultipleComponent]
  public class Gaze : MonoBehaviour
  {
    // How often she checks who acts nearby, in seconds.
    private const float WatchEvery = 0.2f;

    // An asked look or a cue holds her glances this much closer than her own look.
    private const float FocusedGlance = 0.4f;

    // The share of glances that come back to the middle.
    private const float RecentreChance = 0.25f;

    [Tooltip("The point her eyes aim at (EyeAim's target). Empty = the HumonoidCharacter's eye target.")]
    [SerializeField] private Transform _target;

    [Tooltip("Who acts around her; empty = she never glances at anyone.")]
    [SerializeField] private AttentionSO _attention;

    [Header("Her own look")]
    [Tooltip("Seconds between idle glances, picked at random in this range.")]
    [SerializeField] private Vector2 _glanceEvery = new(0.5f, 2.2f);

    [Tooltip("How far a small glance strays from where she looks, in degrees.")]
    [SerializeField, Range(0f, 30f)] private float _glanceDegrees = 6f;

    [Tooltip("The chance a glance is a bigger look around.")]
    [SerializeField, Range(0f, 1f)] private float _lookAroundChance = 0.15f;

    [Tooltip("How far a look around strays, in degrees.")]
    [SerializeField, Range(0f, 45f)] private float _lookAroundDegrees = 16f;

    [Tooltip("How long one eye jump takes, in seconds.")]
    [SerializeField, Min(0.01f)] private float _saccadeSeconds = 0.05f;

    [Header("Blinks")]
    [Tooltip("An eye jump wider than this blinks, in degrees; 0 never does.")]
    [SerializeField, Min(0f)] private float _blinkOverDegrees = 14f;

    [Header("Who acts")]
    [Tooltip("How far she notices who acts, in metres.")]
    [SerializeField, Min(0f)] private float _noticeRadius = 8f;

    [Tooltip("How long she takes to react to a cue, in seconds, picked at random in this range.")]
    [SerializeField] private Vector2 _noticeDelay = new(0.12f, 0.3f);

    [Tooltip("How much her head turns to a cue, at full strength (0 = eyes only).")]
    [SerializeField, Range(0f, 1f)] private float _noticeHead = 0.45f;

    [Tooltip("The longest she keeps looking at one cue, in seconds.")]
    [SerializeField, Min(0.1f)] private float _noticeMaxSeconds = 2.5f;

    private enum Kind
    {
      Own,
      Asked,
      Noticed,
    }

    // What she looks at: her own look, or a target or a point until a time (0 = until released).
    private readonly struct Look
    {
      public readonly Kind Kind;
      public readonly Transform Follow;
      public readonly bool Following;
      public readonly Vector3 Point;
      public readonly float Until;
      public readonly float Head;

      public Look(Kind kind, Transform follow, Vector3 point, float until, float head)
      {
        Kind = kind;
        Follow = follow;
        Following = follow != null;
        Point = point;
        Until = until;
        Head = head;
      }

      public static Look Own => new(Kind.Own, null, Vector3.zero, 0f, 0f);
    }

    private FaceFrame _frame;
    private Blinker _blinker;
    private HeadLook _headLook;
    private bool _headOwned;
    private Vector3 _restLocal;
    private bool _ready;

    private Look _look = Look.Own;
    private Quaternion _glance = Quaternion.identity;
    private float _nextGlance;

    private Vector3 _direction;
    private Vector3 _jumpFrom;
    private float _jumpStart = -1f;

    private float _nextWatch;
    private Transform _pending;
    private float _pendingAt;
    private float _pendingUntil;
    private float _pendingHead;

    /// <summary>Who acts around her (null: she never glances at anyone).</summary>
    public AttentionSO Attention
    {
      get => _attention;
      set => _attention = value;
    }

    /// <summary>What she follows now (null for a point or her own look).</summary>
    public Transform Following => _look.Kind == Kind.Own ? null : _look.Follow;

    /// <summary>Whether a look someone asked for holds her eyes.</summary>
    public bool IsAsked => _look.Kind == Kind.Asked;

    /// <summary>
    /// Look at <paramref name="target"/> as it moves, for <paramref name="seconds"/> (0 = until
    /// <see cref="Release"/>), her head turning by <paramref name="head"/> (0 = eyes only).
    /// </summary>
    public void LookAt(Transform target, float seconds = 0f, float head = 0f)
    {
      if (target == null) throw new System.ArgumentNullException(nameof(target));
      Focus(Kind.Asked, target, Vector3.zero, seconds, head);
    }

    /// <summary>Look at <paramref name="point"/> for <paramref name="seconds"/> (0 = until <see cref="Release"/>).</summary>
    public void LookAt(Vector3 point, float seconds, float head = 0f) => Focus(Kind.Asked, null, point, seconds, head);

    /// <summary>Look at the main camera (the player) for <paramref name="seconds"/> (0 = until <see cref="Release"/>).</summary>
    public void LookAtCamera(float seconds = 0f, float head = 0f)
    {
      Camera camera = Camera.main;
      if (camera == null) return;
      LookAt(camera.transform, seconds, head);
    }

    /// <summary>Back to her own look.</summary>
    public void Release()
    {
      if (!_ready || _look.Kind == Kind.Own) return;
      ChangeTo(Look.Own, Quaternion.identity);
      StopHead();
    }

    private void Awake()
    {
      if (_target == null)
      {
        HumonoidCharacter character = GetComponentInParent<HumonoidCharacter>();
        if (character == null) character = GetComponentInChildren<HumonoidCharacter>(true);
        if (character != null) _target = character.EyeTarget;
      }

      _blinker = GetComponentInChildren<Blinker>(true);
      _headLook = GetComponentInChildren<HeadLook>(true);
      _frame = FaceFrame.Of(transform);

      // A rig without eye bones or an eye target (an older model) has no eyes to move.
      if (_target == null || _frame == null) return;
      _restLocal = _target.localPosition;
      _ready = true;
    }

    private void OnEnable()
    {
      if (!_ready) return;
      _look = Look.Own;
      _pending = null;
      _glance = Quaternion.identity;
      _jumpStart = -1f;
      _nextGlance = Time.time + Random.Range(_glanceEvery.x, _glanceEvery.y);
      _frame.Refresh();
      _direction = Wanted();
    }

    private void OnDisable()
    {
      if (!_ready) return;
      _target.localPosition = _restLocal;
      StopHead();
    }

    private void Update()
    {
      if (!_ready) return;
      float now = Time.time;
      _frame.Refresh();

      EndLookWhenDone(now);
      WatchWhoActs(now);
      Glance(now);
      Steer(now);
    }

    // A look ends when its time is up or what it follows went away.
    private void EndLookWhenDone(float now)
    {
      if (_look.Kind == Kind.Own) return;
      bool gone = _look.Following && (_look.Follow == null || !_look.Follow.gameObject.activeInHierarchy);
      if (gone || (_look.Until > 0f && now >= _look.Until)) Release();
    }

    private void WatchWhoActs(float now)
    {
      if (_attention == null || _look.Kind == Kind.Asked) return;

      if (_pending != null && now >= _pendingAt)
      {
        Transform pending = _pending;
        _pending = null;
        if (pending.gameObject.activeInHierarchy) Focus(Kind.Noticed, pending, Vector3.zero, Mathf.Max(0.1f, _pendingUntil - now), _pendingHead);
      }

      if (now < _nextWatch) return;
      _nextWatch = now + WatchEvery;
      if (!_attention.TryBest(_frame.Origin, gameObject, _noticeRadius, out AttentionCue cue)) return;
      if (cue.Source == _look.Follow || cue.Source == _pending) return;

      _pending = cue.Source;
      _pendingAt = now + Random.Range(_noticeDelay.x, _noticeDelay.y);
      _pendingUntil = Mathf.Min(cue.Until, now + _noticeMaxSeconds);
      _pendingHead = _noticeHead * cue.Strength;
    }

    private void Glance(float now)
    {
      if (now < _nextGlance) return;
      _nextGlance = now + Random.Range(_glanceEvery.x, _glanceEvery.y);

      bool own = _look.Kind == Kind.Own;
      float roll = Random.value;
      float degrees;
      if (roll < RecentreChance) degrees = 0f;
      else if (own && roll < RecentreChance + _lookAroundChance) degrees = _lookAroundDegrees;
      else degrees = _glanceDegrees * (own ? 1f : FocusedGlance);

      Vector2 offset = Random.insideUnitCircle * degrees;
      ChangeTo(_look, Quaternion.AngleAxis(offset.x, _frame.Up) * Quaternion.AngleAxis(offset.y * 0.5f, _frame.Right));
    }

    // Moves the eye target: through the current jump, then straight on what she looks at.
    private void Steer(float now)
    {
      Vector3 wanted = Wanted();
      if (_jumpStart >= 0f)
      {
        float t = (now - _jumpStart) / _saccadeSeconds;
        if (t >= 1f) _jumpStart = -1f;
        _direction = t >= 1f ? wanted : Vector3.Slerp(_jumpFrom, wanted, t * t * (3f - 2f * t));
      }
      else
      {
        _direction = wanted;
      }

      float distance = Mathf.Max(0.05f, Vector3.Distance(_frame.Origin, FocusPoint()));
      _target.position = _frame.Origin + _direction * distance;
    }

    private void Focus(Kind kind, Transform follow, Vector3 point, float seconds, float head)
    {
      if (!_ready) return;
      head = Mathf.Clamp01(head);
      ChangeTo(new Look(kind, follow, point, seconds > 0f ? Time.time + seconds : 0f, head), Quaternion.identity);

      if (_headLook == null || head <= 0f)
      {
        StopHead();
        return;
      }

      if (follow != null) _headLook.Follow(follow, head);
      else _headLook.LookAt(point, head);
      _headOwned = true;
    }

    // Every change of where she looks is one saccade from where her eyes are now; a wide one blinks.
    private void ChangeTo(Look look, Quaternion glance)
    {
      _jumpFrom = _direction;
      _look = look;
      _glance = glance;
      _jumpStart = Time.time;
      if (_blinker != null && _blinkOverDegrees > 0f && Vector3.Angle(_jumpFrom, Wanted()) > _blinkOverDegrees) _blinker.BlinkNow();
    }

    private void StopHead()
    {
      if (!_headOwned) return;
      _headOwned = false;
      if (_headLook != null) _headLook.Stop();
    }

    private Vector3 Wanted() => _glance * Direction(FocusPoint());

    private Vector3 FocusPoint()
    {
      if (_look.Kind == Kind.Own) return Rest();
      if (_look.Following) return _look.Follow != null ? _look.Follow.position : Rest();
      return _look.Point;
    }

    private Vector3 Rest() => _target.parent != null ? _target.parent.TransformPoint(_restLocal) : _restLocal;

    private Vector3 Direction(Vector3 point)
    {
      Vector3 to = point - _frame.Origin;
      return to.sqrMagnitude > 1e-8f ? to.normalized : _frame.Forward;
    }
  }
}
