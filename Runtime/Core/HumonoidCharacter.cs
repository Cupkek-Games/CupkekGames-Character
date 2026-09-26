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
    /// <summary>The point EyeMovement moves and EyeAim aims the eyes at.</summary>
    public Transform EyeTarget => _lookAtTarget;
    private IAnimationStateController _animationController;
    public IAnimationStateController AnimationController => _animationController;
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
      _face = GetComponentInChildren<FaceController>();
      _animationController = GetComponentInChildren<IAnimationStateController>();
      _animationEngine = GetComponentInChildren<IAnimationEngine>();

      if (_head == null)
      {
        // The humanoid bone, never a name search: a mesh can share a bone's name.
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman) _head = animator.GetBoneTransform(HumanBodyBones.Head);
      }
    }

    private void OnEnable()
    {
      // Create natural eye movement with constructor
      if (_lookAtTarget != null)
      {
        _eyeMovement = new EyeMovement(_lookAtTarget, this, _animationController);
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
      if (_animationController != null)
      {
        Tween.LocalPosition(_animationController.Transform, Vector3.zero, 0.2f);
      }
    }

    /// <summary>
    /// Play the expression registered under <paramref name="key"/>, returning to rest after
    /// <paramref name="holdSeconds"/> (0 or less holds until the next expression), with the
    /// key's emote VFX if <see cref="EmoteVFXDatabase"/> has one. An unknown key throws; a
    /// character with no face rig yet plays only the emote.
    /// </summary>
    public void PlayExpression(string key, float holdSeconds = 2f)
    {
      FaceExpressionSO expression = FaceExpressions.GetRequired(key);
      if (_face != null) _face.Play(expression, holdSeconds);

      PlayEmote(key, holdSeconds).Forget();
    }

    private async UniTaskVoid PlayEmote(string key, float holdSeconds)
    {
      EmoteVFXDatabase emotes = ServiceLocator.Get<EmoteVFXDatabase>(true);
      if (emotes == null || _emoteParticleController == null || EmotionTarget == null) return;
      if (!emotes.TryGetValue(key, out VFXBundle vfx) || vfx == null) return;

      int durationMs = (int)(Mathf.Max(holdSeconds, 0.5f) * 1000);
      await _emoteParticleController.PlayParticle(vfx, gameObject, EmotionTarget, durationMs, TimeManager.Instance);
    }

    public void PlayAnimation(AnimationClip clip, float fadeDuration = 0.25f)
    {
      if (_animationController == null || clip == null)
      {
        return;
      }

      _animationController.PlayClipWithReturnToIdle(clip, fadeDuration);
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