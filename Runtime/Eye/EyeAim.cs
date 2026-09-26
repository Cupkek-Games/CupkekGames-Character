using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// Aims both eyes at a target after animation, in parallel: one rotation, computed from
  /// the eyes' shared forward, is applied to both, so near targets never cross the eyes and
  /// both move at the same speed. The rotation is clamped to <see cref="MaxAngle"/>, past
  /// which a stylized eye clips through the head. The target is usually the look-at point
  /// that <see cref="EyeMovement"/> drives.
  /// </summary>
  [DisallowMultipleComponent]
  public class EyeAim : MonoBehaviour
  {
    [Tooltip("Empty = the Animator's humanoid LeftEye bone.")]
    [SerializeField] private Transform _leftEye;

    [Tooltip("Empty = the Animator's humanoid RightEye bone.")]
    [SerializeField] private Transform _rightEye;

    [Tooltip("Empty = the HumonoidCharacter's look-at target.")]
    [SerializeField] private Transform _target;

    [Tooltip("The eye bones' local axis that points out of the pupil.")]
    [SerializeField] private Vector3 _aimAxis = Vector3.up;

    [SerializeField, Range(0f, 45f)] private float _maxAngle = 9f;
    [SerializeField, Range(0f, 1f)] private float _weight = 1f;

    private Quaternion _leftRest;
    private Quaternion _rightRest;
    private bool _resolved;

    public Transform LeftEye => _leftEye;
    public Transform RightEye => _rightEye;
    public Transform Target => _target;
    public float MaxAngle => _maxAngle;

    public float Weight
    {
      get => _weight;
      set => _weight = Mathf.Clamp01(value);
    }

    /// <summary>Fill empty eye and target fields from the Animator and the HumonoidCharacter.</summary>
    public bool Resolve()
    {
      if (_leftEye == null || _rightEye == null)
      {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
          if (_leftEye == null) _leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
          if (_rightEye == null) _rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
        }
      }

      if (_target == null)
      {
        HumonoidCharacter character = GetComponentInParent<HumonoidCharacter>();
        if (character == null) character = GetComponentInChildren<HumonoidCharacter>();
        if (character != null) _target = character.EyeTarget;
      }

      return _leftEye != null && _rightEye != null && _target != null;
    }

    private void OnEnable()
    {
      _resolved = Resolve();
      if (!_resolved) return;

      _leftRest = _leftEye.localRotation;
      _rightRest = _rightEye.localRotation;
    }

    private void LateUpdate()
    {
      if (!_resolved) return;

      // Start from rest every frame: rigs whose Animator doesn't write the eye bones
      // would otherwise stack this frame's rotation on the last one.
      _leftEye.localRotation = _leftRest;
      _rightEye.localRotation = _rightRest;
      if (_weight <= 0f) return;

      Vector3 forward = _leftEye.rotation * _aimAxis + _rightEye.rotation * _aimAxis;
      Vector3 toTarget = _target.position - (_leftEye.position + _rightEye.position) * 0.5f;
      if (forward.sqrMagnitude < 1e-8f || toTarget.sqrMagnitude < 1e-8f) return;

      Quaternion delta = Quaternion.FromToRotation(forward.normalized, toTarget.normalized);
      delta = Quaternion.RotateTowards(Quaternion.identity, delta, _maxAngle);
      if (_weight < 1f) delta = Quaternion.Slerp(Quaternion.identity, delta, _weight);

      _leftEye.rotation = delta * _leftEye.rotation;
      _rightEye.rotation = delta * _rightEye.rotation;
    }
  }
}
