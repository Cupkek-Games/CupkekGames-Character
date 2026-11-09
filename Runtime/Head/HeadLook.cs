using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// Turns the head, and part of the neck, toward a point after animation, on
  /// top of whatever the body plays: a seated pose that looks at its feet can
  /// still lift its face to the fire or the camera. The face's forward is
  /// measured once in the rest pose (the model as instantiated, facing its
  /// root's forward), so any rig's head axes work. The turn is clamped to
  /// <see cref="MaxAngle"/> and fades in and out over <see cref="BlendSeconds"/>.
  /// The turn is applied after animation or in the Animator's IK pass
  /// (<see cref="Pass"/>, <see cref="HeadLookPass"/>).
  /// Put it on the character's root (the transform whose forward is the face's
  /// in the rest pose), beside its Animator.
  /// </summary>
  [DisallowMultipleComponent]
  public class HeadLook : MonoBehaviour
  {
    // Frames in a row the IK pass may stay silent while a look is on before it is reported.
    private const int SilentIKFrames = 30;

    [Tooltip("Where the turn is applied. After animation works on any rig. The Animator's IK pass turns what rides the head with it (a particle); it needs a humanoid rig with the Animator's IK pass on.")]
    [SerializeField] private HeadLookPass _pass = HeadLookPass.AfterAnimation;

    [Tooltip("Empty = the Animator's humanoid Head bone.")]
    [SerializeField] private Transform _head;

    [Tooltip("Empty = the Animator's humanoid Neck bone (none: the head turns alone).")]
    [SerializeField] private Transform _neck;

    [Tooltip("The neck's share of the turn; the head takes the rest.")]
    [SerializeField, Range(0f, 1f)] private float _neckShare = 0.4f;

    [Tooltip("The most the face turns from where the animation points it, in degrees.")]
    [SerializeField, Range(0f, 90f)] private float _maxAngle = 50f;

    [Tooltip("How long the look takes to fade in or out, in seconds.")]
    [SerializeField, Min(0f)] private float _blendSeconds = 0.4f;

    [Tooltip("In the Animator's IK pass: how much the chest turns with the head (0 = head and neck only).")]
    [SerializeField, Range(0f, 1f)] private float _bodyShare = 0.2f;

    private Animator _animator;
    private HeadLookIKRelay _relay;
    private int _ikFrame = -1;
    private int _silentIK;
    private Vector3 _faceInHead;
    private bool _resolved;
    private Vector3 _point;
    private bool _hasPoint;
    private Transform _follow;
    private float _targetWeight;
    private float _weight;

    /// <summary>
    /// Where the turn is applied (<see cref="HeadLookPass"/>). The Animator's IK
    /// pass needs a humanoid rig and fails loud without one.
    /// </summary>
    public HeadLookPass Pass
    {
      get => _pass;
      set
      {
        _pass = value;
        if (_pass == HeadLookPass.AnimatorIK && Resolve()) BindIKPass();
      }
    }

    public float MaxAngle => _maxAngle;
    public float BlendSeconds => _blendSeconds;

    /// <summary>How much of the look is on now (0 to 1, fading toward its target).</summary>
    public float Weight => _weight;

    /// <summary>Looks at a fixed point, at <paramref name="weight"/> (0 to 1).</summary>
    public void LookAt(Vector3 point, float weight = 1f)
    {
      _follow = null;
      _point = point;
      _hasPoint = true;
      _targetWeight = Mathf.Clamp01(weight);
    }

    /// <summary>Keeps looking at <paramref name="target"/> as it moves, at <paramref name="weight"/> (0 to 1).</summary>
    public void Follow(Transform target, float weight = 1f)
    {
      _follow = target;
      _hasPoint = false;
      _targetWeight = target != null ? Mathf.Clamp01(weight) : 0f;
    }

    /// <summary>The look fades out; the animation has the head again.</summary>
    public void Stop() => _targetWeight = 0f;

    private void Awake()
    {
      if (Resolve() && _pass == HeadLookPass.AnimatorIK) BindIKPass();
    }

    private void OnDestroy()
    {
      if (_relay != null) Destroy(_relay);
    }

    private bool Resolve()
    {
      if (_resolved) return true;
      if (_animator == null) _animator = GetComponentInChildren<Animator>();
      if (_animator != null && _animator.isHuman)
      {
        if (_head == null) _head = _animator.GetBoneTransform(HumanBodyBones.Head);
        if (_neck == null) _neck = _animator.GetBoneTransform(HumanBodyBones.Neck);
      }

      if (_head == null) return false;
      _faceInHead = Quaternion.Inverse(_head.rotation) * transform.forward;
      _resolved = true;
      return true;
    }

    // Unity sends OnAnimatorIK only to the Animator's own object: a relay there hands it on.
    private void BindIKPass()
    {
      if (_relay != null) return;
      if (_animator == null || !_animator.isHuman)
        throw new System.InvalidOperationException($"[HeadLook] {name} turns in the Animator's IK pass, which needs a humanoid Animator; it has none.");
      if (!_animator.TryGetComponent(out _relay)) _relay = _animator.gameObject.AddComponent<HeadLookIKRelay>();
      _relay.Bind(this, _animator);
    }

    // The look in the IK pass, before Unity places what rides the head for the frame.
    internal void ApplyIK(Animator animator)
    {
      _ikFrame = Time.frameCount;
      if (_pass != HeadLookPass.AnimatorIK || !isActiveAndEnabled || _weight <= 0f || !TryTarget(out Vector3 target))
      {
        animator.SetLookAtWeight(0f);
        return;
      }

      animator.SetLookAtWeight(_weight, _bodyShare, 1f, 0f, ClampWeight);
      animator.SetLookAtPosition(target);
    }

    // Unity's look-at clamp: 0 turns freely, 1 not at all, 0.5 through half the circle.
    private float ClampWeight => 1f - _maxAngle / 180f;

    private void LateUpdate()
    {
      float step = _blendSeconds > 0f ? Time.deltaTime / _blendSeconds : 1f;
      _weight = Mathf.MoveTowards(_weight, _targetWeight, step);
      if (_weight <= 0f || !Resolve()) return;
      if (!TryTarget(out Vector3 target)) return;

      if (_pass == HeadLookPass.AnimatorIK)
      {
        ReportSilentIKPass();
        return;
      }

      float total = Clamped(target) * _weight;
      float neck = _neck != null ? _neckShare : 0f;
      if (neck > 0f) Turn(_neck, target, total * neck);
      Turn(_head, target, total * (1f - neck));
    }

    private bool TryTarget(out Vector3 target)
    {
      target = _follow != null ? _follow.position : _point;
      return _follow != null || _hasPoint;
    }

    // The IK pass runs before LateUpdate: a look on with no IK call this frame means the pass is off.
    private void ReportSilentIKPass()
    {
      if (_relay == null || !_animator.isActiveAndEnabled) return;
      _silentIK = _ikFrame == Time.frameCount ? 0 : _silentIK + 1;
      if (_silentIK == SilentIKFrames)
        Debug.LogError($"[HeadLook] {name} turns in the Animator's IK pass, but {_animator.name}'s Animator runs none: turn it on (Animancer: AnimancerGraph.ApplyAnimatorIK; an Animator Controller: its layer's IK Pass), or the Animator is culled off screen.", this);
    }

    // How far the face is from the target, clamped, in degrees.
    private float Clamped(Vector3 target)
    {
      Vector3 face = _head.rotation * _faceInHead;
      return Mathf.Min(Vector3.Angle(face, target - _head.position), _maxAngle);
    }

    // Turns the bone by up to <paramref name="degrees"/>, bringing the face toward the target.
    private void Turn(Transform bone, Vector3 target, float degrees)
    {
      if (degrees <= 0f) return;
      Vector3 face = _head.rotation * _faceInHead;
      Vector3 to = target - _head.position;
      Vector3 axis = Vector3.Cross(face, to);
      if (axis.sqrMagnitude < 1e-8f) return;
      bone.rotation = Quaternion.AngleAxis(Mathf.Min(degrees, Vector3.Angle(face, to)), axis.normalized) * bone.rotation;
    }
  }

  /// <summary>Where <see cref="HeadLook"/> applies its turn.</summary>
  public enum HeadLookPass
  {
    /// <summary>After animation (LateUpdate), on any rig; what rides the head (a particle) does not turn with it.</summary>
    AfterAnimation,

    /// <summary>
    /// In the Animator's IK pass (its look-at), so what rides the head turns with it. Needs a
    /// humanoid rig and the Animator's IK pass on (Animancer: AnimancerGraph.ApplyAnimatorIK;
    /// an Animator Controller: its layer's IK Pass). <see cref="HeadLook.MaxAngle"/> becomes
    /// the look-at's clamp.
    /// </summary>
    AnimatorIK,
  }
}
