using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// The face's own axes, live: the point between the eyes, her right (from her left eye to
  /// her right), up and forward (out of the face), and the gap between the eyes as the unit.
  /// Anything placed on a face (a blush, a sweat drop, a nod) is measured in it, so it fits
  /// every head size and follows the head whatever the animation does. Up and forward are
  /// measured once in the head bone's space while the model stands at rest (as instantiated,
  /// facing its root's forward), so any rig's bone axes work.
  /// </summary>
  public sealed class FaceFrame
  {
    private readonly Transform _leftEye;
    private readonly Transform _rightEye;
    private readonly Transform _head;
    private readonly Vector3 _upInHead;
    private readonly Vector3 _forwardInHead;

    /// <summary>The point between the eyes.</summary>
    public Vector3 Origin { get; private set; }

    /// <summary>Her right, from her left eye to her right.</summary>
    public Vector3 Right { get; private set; }

    public Vector3 Up { get; private set; }

    /// <summary>Out of the face.</summary>
    public Vector3 Forward { get; private set; }

    /// <summary>The gap between the eyes in world units: the frame's unit.</summary>
    public float Gap { get; private set; }

    public Transform Head => _head;
    public Transform LeftEye => _leftEye;
    public Transform RightEye => _rightEye;

    private FaceFrame(Transform leftEye, Transform rightEye, Transform head, Transform root)
    {
      _leftEye = leftEye;
      _rightEye = rightEye;
      _head = head;

      Vector3 right = (rightEye.position - leftEye.position).normalized;
      Vector3 up = Vector3.ProjectOnPlane(root.up, right).normalized;
      Vector3 forward = Vector3.Cross(right, up);
      if (Vector3.Dot(forward, root.forward) < 0f)
      {
        // A rig whose eye bones are named the other way round: trust the root's forward.
        forward = -forward;
      }

      _upInHead = Quaternion.Inverse(head.rotation) * up;
      _forwardInHead = Quaternion.Inverse(head.rotation) * forward;
      Refresh();
    }

    /// <summary>
    /// The face of the humanoid under <paramref name="root"/>, measured now, so call it while
    /// the model stands at rest; null when its rig has no head or no eye bones.
    /// </summary>
    public static FaceFrame Of(Transform root)
    {
      Animator animator = root.GetComponentInChildren<Animator>(true);
      if (animator == null || !animator.isHuman) return null;
      Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
      Transform left = animator.GetBoneTransform(HumanBodyBones.LeftEye);
      Transform right = animator.GetBoneTransform(HumanBodyBones.RightEye);
      if (head == null || left == null || right == null) return null;
      if ((right.position - left.position).sqrMagnitude < 1e-10f) return null;
      return new FaceFrame(left, right, head, root);
    }

    /// <summary>Reads the head's pose now. Call before using the axes in a frame.</summary>
    public void Refresh()
    {
      Vector3 left = _leftEye.position;
      Vector3 right = _rightEye.position;
      Origin = (left + right) * 0.5f;
      Gap = Vector3.Distance(left, right);
      Right = Gap > 1e-6f ? (right - left) / Gap : _head.right;
      Vector3 up = _head.rotation * _upInHead;
      Forward = Vector3.Cross(Right, up).normalized;
      if (Vector3.Dot(Forward, _head.rotation * _forwardInHead) < 0f) Forward = -Forward;
      Up = Vector3.Cross(Forward, Right).normalized;
    }

    /// <summary>A point given in eye gaps: x her right, y up, z out of the face.</summary>
    public Vector3 Point(Vector3 gaps) => Origin + (Right * gaps.x + Up * gaps.y + Forward * gaps.z) * Gap;

    /// <summary>A direction given in the face's axes.</summary>
    public Vector3 Direction(Vector3 local) => Right * local.x + Up * local.y + Forward * local.z;
  }
}
