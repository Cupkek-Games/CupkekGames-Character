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
    private BlendShapeController _blendShapeController;
    public BlendShapeController BlendShapeController => _blendShapeController;
    [SerializeField] private Transform _lookAtTarget;
    private ILocomotion _locomotion;
    public ILocomotion Locomotion => _locomotion;
    private IAnimationEngine _animationEngine;
    public IAnimationEngine AnimationEngine => _animationEngine;
    [SerializeField] private Transform _head;
    public Transform Head => _head;
    [SerializeField] private Transform _emotionTarget;
    public Transform EmotionTarget => _emotionTarget;

    private EyeMovement _eyeMovement;
    public EyeMovement EyeMovement => _eyeMovement;

    private EmoteParticleController _emoteParticleController;

    public void Awake()
    {
      _blendShapeController = GetComponentInChildren<BlendShapeController>();
      _locomotion = GetComponentInChildren<ILocomotion>();
      _animationEngine = GetComponentInChildren<IAnimationEngine>();
    }

    private void OnEnable()
    {
      // Create natural eye movement with constructor
      if (_lookAtTarget != null)
      {
        _eyeMovement = new EyeMovement(_lookAtTarget, this, _locomotion);
      }
      else
      {
        Debug.LogError("Look At Target is not set on " + name);
      }

      // Create emote particle controller
      _emoteParticleController = new EmoteParticleController();
    }

    private void OnDisable()
    {
      if (_eyeMovement != null)
      {
        _eyeMovement.Dispose();
        _eyeMovement = null;
      }

      if (_emoteParticleController != null)
      {
        _emoteParticleController.Dispose();
        _emoteParticleController = null;
      }
    }

    public void ResetAnimatorPosition()
    {
      if (_locomotion != null)
      {
        Tween.LocalPosition(_locomotion.transform, Vector3.zero, 0.2f);
      }
    }

    private void AutoFindReferences()
    {
      AutoFindLoopChildren(transform);
    }

    private void AutoFindLoopChildren(Transform parent)
    {
      foreach (Transform child in parent)
      {
        if (child.name == "Head")
        {
          _head = child;
        }

        if (child.name == "Look At")
        {
          _lookAtTarget = child;
        }

        if (_head != null && _lookAtTarget != null)
        {
          break;
        }

        // Recursively call LoopChildren to handle nested children
        AutoFindLoopChildren(child);
      }
    }

    public async UniTaskVoid PlayExpression(BlendShapeDatabase blendShapeDatabase, BlendShapeEnum expression,
      float expressionDuration = 2f)
    {
      if (blendShapeDatabase == null)
      {
        blendShapeDatabase = ServiceLocator.Get<BlendShapeDatabase>();
      }

      if (BlendShapeController == null)
      {
        Debug.LogWarning($"BlendShapeController is null on {gameObject.name}");
        return;
      }

      VFXBundle vfx = blendShapeDatabase.GetVFX(expression);

      // Use SetTargetSO to fire pre-expression event
      BlendShapeController.SetTargetSO(blendShapeDatabase.GetByType(expression), expression);
      BlendShapeController.BlendToTarget(0.5f);

      // Schedule return to neutral after expression duration
      if (expressionDuration > 0)
      {
        BlendShapeController.BlendToNewTargetWithDelay(
          blendShapeDatabase.GetByType(BlendShapeEnum.Neutral), 0.5f, expressionDuration, BlendShapeEnum.Neutral);
      }

      if (vfx != null && _emoteParticleController != null)
      {
        TimeManager timeManager = TimeManager.Instance;
        int durationMs = (int)(expressionDuration * 1000);
        await _emoteParticleController.PlayParticle(vfx, gameObject, EmotionTarget, durationMs, timeManager);
      }
    }

    public void PlayAnimation(AnimationClip clip, float fadeDuration = 0.25f)
    {
      if (_locomotion == null || clip == null)
      {
        return;
      }

      _locomotion.PlayClipWithReturnToIdle(clip, fadeDuration);
    }

    #region Eye Movement Convenience Methods

    /// <summary>
    /// Makes the character look at the main camera
    /// </summary>
    /// <param name="fadeDuration">How long the fade in/out should take (default: 0.4f)</param>
    /// <param name="duration">How long to follow the camera (0 = follow indefinitely until reset)</param>
    public void LookAtCamera(float fadeDuration = 0.4f, float duration = 0f)
    {
      if (_eyeMovement == null || Camera.main == null)
      {
        return;
      }

      _eyeMovement.LookAtTargetFollow(Camera.main.transform, fadeDuration, duration);
    }

    /// <summary>
    /// Makes the character look at a specific transform target
    /// </summary>
    /// <param name="target">The transform to look at</param>
    /// <param name="fadeDuration">How long the fade in/out should take (default: 0.4f)</param>
    /// <param name="duration">How long to follow the target (0 = follow indefinitely until reset)</param>
    public void LookAtTarget(Transform target, float fadeDuration = 0.4f, float duration = 0f)
    {
      if (_eyeMovement == null || target == null)
      {
        return;
      }

      _eyeMovement.LookAtTargetFollow(target, fadeDuration, duration);
    }

    /// <summary>
    /// Makes the character look at a specific position for a duration
    /// </summary>
    /// <param name="position">The world position to look at</param>
    /// <param name="fadeDuration">How long the fade in/out should take (default: 0.4f)</param>
    /// <param name="duration">How long to look at the position</param>
    public void LookAtPosition(Vector3 position, float fadeDuration = 0.4f, float duration = 2f)
    {
      if (_eyeMovement == null)
      {
        return;
      }

      _eyeMovement.LookAtTarget(position, fadeDuration, duration);
    }

    /// <summary>
    /// Resets the character's eye movement to natural behavior
    /// </summary>
    public void ResetEyeMovement()
    {
      if (_eyeMovement == null)
      {
        return;
      }

      // Important: LookAtTargetFollow(duration: 0) uses a follow coroutine that must be stopped explicitly,
      // otherwise the NPC will keep looking at the camera even after returning to overview.
      _eyeMovement.StopFollowingTarget(0.4f, restoreNaturalEyeMovement: true);
    }

    #endregion
  }
}