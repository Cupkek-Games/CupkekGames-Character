using System;
using CupkekGames.TimeSystem;
using CupkekGames.AddressableAssets;
using CupkekGames.SceneManagement;
using CupkekGames.Sequencer;
using CupkekGames.Services;
using CupkekGames.Settings;
using CupkekGames.GameSave;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using CupkekGames.Animations;

using CupkekGames.VFX;

namespace CupkekGames.Character
{
  public class HumonoidCharacter : MonoBehaviour
  {
    private FaceController _face;

    /// <summary>The face, or null on a character without a face rig yet.</summary>
    public FaceController Face => _face;

    [SerializeField] private Transform _lookAtTarget;
    /// <summary>The point <see cref="Gaze"/> moves and EyeAim aims the eyes at.</summary>
    public Transform EyeTarget => _lookAtTarget;
    private IAnimationStateController _animationController;
    public IAnimationStateController AnimationController => _animationController;
    private IAnimationEngine _animationEngine;
    public IAnimationEngine AnimationEngine => _animationEngine;
    [SerializeField] private Transform _head;
    public Transform Head => _head;
    [SerializeField] private Transform _emotionTarget;
    public Transform EmotionTarget => _emotionTarget;

    private Gaze _gaze;

    /// <summary>Where she looks (null on a character without one).</summary>
    public Gaze Gaze => _gaze;

    public void Awake()
    {
      _face = GetComponentInChildren<FaceController>();
      _animationController = GetComponentInChildren<IAnimationStateController>();
      _animationEngine = GetComponentInChildren<IAnimationEngine>();
      _gaze = GetComponentInChildren<Gaze>(true);

      if (_head == null)
      {
        // The humanoid bone, never a name search: a mesh can share a bone's name.
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman) _head = animator.GetBoneTransform(HumanBodyBones.Head);
      }
    }

    public void ResetAnimatorPosition()
    {
      if (_animationController != null)
      {
        Tween.LocalPosition(_animationController.Transform, Vector3.zero, 0.2f);
      }
    }

    public void PlayAnimation(AnimationClip clip, float fadeDuration = 0.25f)
    {
      if (_animationController == null || clip == null)
      {
        return;
      }

      _animationController.PlayClipWithReturnToIdle(clip, fadeDuration);
    }
  }
}