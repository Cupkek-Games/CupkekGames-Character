using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// Hands the Animator's IK pass to a <see cref="HeadLook"/> on another object: Unity sends
  /// OnAnimatorIK only to the Animator's own object. The HeadLook adds it at runtime.
  /// </summary>
  [AddComponentMenu("")]
  [DisallowMultipleComponent]
  internal sealed class HeadLookIKRelay : MonoBehaviour
  {
    private HeadLook _look;
    private Animator _animator;

    internal void Bind(HeadLook look, Animator animator)
    {
      _look = look;
      _animator = animator;
    }

    private void OnAnimatorIK(int layerIndex)
    {
      if (_look != null) _look.ApplyIK(_animator);
    }
  }
}
