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
  /// Put it on the character's root (the transform whose forward is the face's
  /// in the rest pose), beside its Animator.
  /// </summary>
  [DisallowMultipleComponent]
  public class HeadLook : MonoBehaviour
  {
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

    private Vector3 _faceInHead;
    private bool _resolved;
    private Vector3 _point;
    private bool _hasPoint;
    private Transform _follow;
    private float _targetWeight;
    private float _weight;

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

    private void Awake() => Resolve();

    private bool Resolve()
    {
      if (_resolved) return true;
      if (_head == null || _neck == null)
      {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
          if (_head == null) _head = animator.GetBoneTransform(HumanBodyBones.Head);
          if (_neck == null) _neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        }
      }

      if (_head == null) return false;
      _faceInHead = Quaternion.Inverse(_head.rotation) * transform.forward;
      _resolved = true;
      return true;
    }

    private void LateUpdate()
    {
      float step = _blendSeconds > 0f ? Time.deltaTime / _blendSeconds : 1f;
      _weight = Mathf.MoveTowards(_weight, _targetWeight, step);
      if (_weight <= 0f || !Resolve()) return;
      if (_follow == null && !_hasPoint) return;

      Vector3 target = _follow != null ? _follow.position : _point;
      float total = Clamped(target) * _weight;
      float neck = _neck != null ? _neckShare : 0f;
      if (neck > 0f) Turn(_neck, target, total * neck);
      Turn(_head, target, total * (1f - neck));
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
}
