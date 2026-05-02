using UnityEngine;
using PrimeTween;
using System.Collections;
using CupkekGames.Animations;

namespace CupkekGames.Character
{
  /// <summary>
  /// Handles natural eye movement for characters, including micro-saccades and attention shifts
  /// </summary>
  public class EyeMovement : System.IDisposable
  {
    // Minimum distance threshold for position changes to avoid unnecessary tweens
    private const float POSITION_EPSILON = 0.001f;

    [System.Serializable]
    public class EyeMovementSettings
    {
      [Header("Micro-saccades (Small Random Movements)")]
      public bool enableMicroSaccades = false;

      public float microSaccadeRange = 0.025f;
      public float microSaccadeFrequency = 0.25f;

      [Header("Attention Shifts (Looking Around)")]
      public bool enableAttentionShifts = true;

      public float attentionShiftChance = 0.7f; // 70% chance when interval is reached
      public float attentionShiftRange = 0.25f;
      public float attentionShiftInterval = 2f;

      [Header("General Settings")] public float eyeMovementSpeed = 1.8f;
      public float rangeMultiplier = 1.2f;
    }

    private EyeMovementSettings _settings;
    private Transform _controller;
    private MonoBehaviour _coroutineRunner;
    private ILocomotion _locomotion;

    private Vector3 _originalLookAtPosition;
    private Vector3 _currentTargetPosition;

    private bool _isNaturalEyeMovementActive = false;
    private Coroutine _naturalEyeMovementCoroutine;
    private Coroutine _followTargetCoroutine;
    private object _currentEyeMovementTween;
    private float _lastAttentionShiftTime;
    private float _animationDelayEndTime;
    private bool _isFollowingTarget = false;
    private bool _wasNaturalEyeMovementActiveBeforeFollow = false;

    /// <summary>
    /// Stops the current eye movement tween/sequence
    /// </summary>
    private void StopCurrentEyeMovement()
    {
      if (_currentEyeMovementTween != null)
      {
        if (_currentEyeMovementTween is Tween tween && tween.isAlive)
          tween.Stop();
        else if (_currentEyeMovementTween is Sequence sequence && sequence.isAlive)
          sequence.Stop();
      }
    }

    /// <summary>
    /// Checks if the current eye movement is alive
    /// </summary>
    private bool IsCurrentEyeMovementAlive()
    {
      if (_currentEyeMovementTween == null) return false;

      if (_currentEyeMovementTween is Tween tween)
        return tween.isAlive;
      else if (_currentEyeMovementTween is Sequence sequence)
        return sequence.isAlive;

      return false;
    }

    /// <summary>
    /// Constructor for the natural eye movement system
    /// </summary>
    /// <param name="controller">The transform that will be moved for eye movement</param>
    /// <param name="coroutineRunner">MonoBehaviour to run coroutines</param>
    /// <param name="animatorController">AnimatorController to listen for animation events</param>
    /// <param name="settings">Eye movement settings</param>
    public EyeMovement(Transform controller, MonoBehaviour coroutineRunner,
      ILocomotion locomotion = null, EyeMovementSettings settings = null)
    {
      _controller = controller;
      _coroutineRunner = coroutineRunner;
      _locomotion = locomotion;
      _settings = settings ?? new EyeMovementSettings();

      if (_controller != null)
      {
        _originalLookAtPosition = _controller.localPosition;
        _currentTargetPosition = _originalLookAtPosition;
      }

      // Register to animation events
      if (_locomotion != null)
      {
        _locomotion.OnAnimationPlayed += OnAnimationPlayed;
      }

      if (_controller != null)
      {
        StartNaturalEyeMovement();
      }
    }

    /// <summary>
    /// Makes the character look at a target position for a specified duration
    /// </summary>
    /// <param name="targetPosition">The world position to look at</param>
    /// <param name="fadeDuration">How long the fade in/out should take</param>
    /// <param name="duration">How long to look at the target in seconds</param>
    /// <returns>The sequence that can be stored and stopped if needed</returns>
    public Sequence LookAtTarget(Vector3 targetPosition, float fadeDuration, float duration)
    {
      if (_controller == null)
      {
        Debug.LogWarning("Look At Target is null. Cannot perform look at action.");
        return Sequence.Create();
      }

      StopNaturalEyeMovement();

      // Stop any current tween
      StopCurrentEyeMovement();

      // Create sequence
      Sequence sequence = Sequence.Create();

      // Move look target to target position
      sequence.Chain(Tween.Position(_controller, targetPosition, fadeDuration, Ease.InOutSine));

      // Wait for the specified duration
      sequence.ChainDelay(duration);

      // Move look target back to original local position
      sequence.Chain(Tween.LocalPosition(_controller, _originalLookAtPosition, fadeDuration, Ease.InOutSine));

      sequence.ChainCallback(StartNaturalEyeMovement);

      return sequence;
    }

    /// <summary>
    /// Makes the character look at a specific target transform and follows it during animation delays
    /// </summary>
    /// <param name="targetTransform">The transform to follow</param>
    /// <param name="fadeDuration">How long the fade in/out should take</param>
    /// <param name="duration">How long to follow the target (0 = follow until target is null)</param>
    public void LookAtTargetFollow(Transform targetTransform, float fadeDuration, float duration)
    {
      if (_controller == null || targetTransform == null)
        return;

      // Stop any previous follow coroutine (prevents multiple follow loops running)
      if (_followTargetCoroutine != null && _coroutineRunner != null)
      {
        _coroutineRunner.StopCoroutine(_followTargetCoroutine);
        _followTargetCoroutine = null;
      }

      // Set follow mode
      _isFollowingTarget = true;

      // Stop natural eye movement temporarily
      bool wasNaturalEyeMovementActive = _isNaturalEyeMovementActive;
      _wasNaturalEyeMovementActiveBeforeFollow = wasNaturalEyeMovementActive;
      StopNaturalEyeMovement();

      // Smooth transition to target at start
      Vector3 localTarget = _coroutineRunner.transform.InverseTransformPoint(targetTransform.position);
      _currentTargetPosition = localTarget;

      if (Vector3.Distance(_controller.localPosition, localTarget) > POSITION_EPSILON)
      {
        StopCurrentEyeMovement();
        _currentEyeMovementTween = Tween.LocalPosition(_controller, localTarget, fadeDuration, Ease.InOutSine);
      }

      // Start following the target
      _followTargetCoroutine = _coroutineRunner.StartCoroutine(
        FollowTargetCoroutine(targetTransform, wasNaturalEyeMovementActive, fadeDuration, duration));
    }

    /// <summary>
    /// Stops following a target transform (started via LookAtTargetFollow) and optionally restores natural movement.
    /// </summary>
    /// <param name="fadeDuration">How long the fade back to original should take</param>
    /// <param name="restoreNaturalEyeMovement">Whether to restore natural eye movement after stopping follow</param>
    public void StopFollowingTarget(float fadeDuration = 0.4f, bool restoreNaturalEyeMovement = true)
    {
      if (_coroutineRunner == null)
        return;

      if (_followTargetCoroutine != null)
      {
        _coroutineRunner.StopCoroutine(_followTargetCoroutine);
        _followTargetCoroutine = null;
      }

      _isFollowingTarget = false;
      _currentTargetPosition = _originalLookAtPosition;

      // Smoothly return to original position
      if (_controller != null && _controller.gameObject.activeInHierarchy)
      {
        StopCurrentEyeMovement();

        if (Vector3.Distance(_controller.localPosition, _originalLookAtPosition) > POSITION_EPSILON)
        {
          _currentEyeMovementTween =
            Tween.LocalPosition(_controller, _originalLookAtPosition, fadeDuration, Ease.InOutSine);
        }
      }

      if (restoreNaturalEyeMovement && !_isNaturalEyeMovementActive)
      {
        StartNaturalEyeMovement();
      }
    }

    #region Natural Eye Movement

    /// <summary>
    /// Starts the natural eye movement system
    /// </summary>
    public void StartNaturalEyeMovement()
    {
      if (_controller == null || _isNaturalEyeMovementActive || _coroutineRunner == null)
        return;

      _isNaturalEyeMovementActive = true;
      _naturalEyeMovementCoroutine = _coroutineRunner.StartCoroutine(NaturalEyeMovementCoroutine());
    }

    /// <summary>
    /// Stops the natural eye movement system
    /// </summary>
    public void StopNaturalEyeMovement()
    {
      if (!_isNaturalEyeMovementActive)
        return;

      _isNaturalEyeMovementActive = false;

      if (_naturalEyeMovementCoroutine != null && _coroutineRunner != null)
      {
        _coroutineRunner.StopCoroutine(_naturalEyeMovementCoroutine);
        _naturalEyeMovementCoroutine = null;
      }

      StopCurrentEyeMovement();

      // Return to original position
      if (_controller != null && _controller.gameObject.activeInHierarchy)
      {
        // Only tween if the target position is different from current position
        if (Vector3.Distance(_controller.localPosition, _originalLookAtPosition) > POSITION_EPSILON)
        {
          _currentEyeMovementTween = Tween.LocalPosition(_controller, _originalLookAtPosition, 0.5f, Ease.InOutSine);
        }
      }
    }

    /// <summary>
    /// Main coroutine for natural eye movement
    /// </summary>
    private IEnumerator NaturalEyeMovementCoroutine()
    {
      _lastAttentionShiftTime = Time.time;

      while (_isNaturalEyeMovementActive)
      {
        // Micro-saccades (small random movements) - only if enabled
        if (_settings.enableMicroSaccades && Random.Range(0f, 1f) < _settings.microSaccadeFrequency)
        {
          PerformMicroSaccade();
        }

        // Attention shifts (time-based instead of random per frame) - only if enabled
        if (_settings.enableAttentionShifts)
        {
          // Don't perform attention shifts during animation delays
          bool isInAnimationDelay = Time.time < _animationDelayEndTime;

          if (!isInAnimationDelay)
          {
            float timeSinceLastAttentionShift = Time.time - _lastAttentionShiftTime;
            if (timeSinceLastAttentionShift >= _settings.attentionShiftInterval)
            {
              // Check chance before performing attention shift
              if (Random.Range(0f, 1f) < _settings.attentionShiftChance)
              {
                PerformAttentionShift();
              }

              _lastAttentionShiftTime = Time.time;
            }
          }
        }

        yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
      }
    }

    /// <summary>
    /// Performs a micro-saccade (small random eye movement)
    /// </summary>
    private void PerformMicroSaccade()
    {
      if (_controller == null || !_controller.gameObject.activeInHierarchy)
        return;

      Vector3 randomOffset = new Vector3(
        Random.Range(-_settings.microSaccadeRange, _settings.microSaccadeRange),
        Random.Range(-_settings.microSaccadeRange * 0.5f, _settings.microSaccadeRange * 0.5f),
        Random.Range(-_settings.microSaccadeRange, _settings.microSaccadeRange)
      );
      randomOffset *= _settings.rangeMultiplier;

      Vector3 saccadeTarget = _currentTargetPosition + randomOffset;

      StopCurrentEyeMovement();

      // Create a sequence for the micro-saccade with settle-back behavior
      Sequence saccadeSequence = Sequence.Create();

      // Move to the saccade target quickly
      saccadeSequence.Chain(Tween.LocalPosition(_controller, saccadeTarget, 0.05f, Ease.InOutSine));

      // Settle back to the original target position with ease-out
      saccadeSequence.Chain(Tween.LocalPosition(_controller, _currentTargetPosition, 0.15f, Ease.InOutSine));

      _currentEyeMovementTween = saccadeSequence;
    }

    /// <summary>
    /// Performs an attention shift to a new area
    /// </summary>
    private void PerformAttentionShift()
    {
      if (_controller == null || !_controller.gameObject.activeInHierarchy)
        return;

      // Generate a new target position within the attention shift range
      Vector3 randomOffset = new Vector3(
        Random.Range(-_settings.attentionShiftRange, _settings.attentionShiftRange),
        Random.Range(-_settings.attentionShiftRange * 0.5f, _settings.attentionShiftRange * 0.5f),
        Random.Range(-_settings.attentionShiftRange, _settings.attentionShiftRange)
      );
      randomOffset *= _settings.rangeMultiplier;

      Vector3 newTargetPosition = _originalLookAtPosition + randomOffset;
      _currentTargetPosition = newTargetPosition;

      StopCurrentEyeMovement();

      _currentEyeMovementTween =
        Tween.LocalPosition(_controller, newTargetPosition, 1f / _settings.eyeMovementSpeed, Ease.InOutSine);
    }

    /// <summary>
    /// Makes the character look at a specific target with natural eye movement
    /// </summary>
    /// <param name="targetPosition">The world position to look at</param>
    /// <param name="duration">How long to look at the target</param>
    public void LookAtTargetWithNaturalMovement(Vector3 targetPosition, float duration)
    {
      if (_controller == null || !_controller.gameObject.activeInHierarchy)
        return;

      // Convert world position to local position relative to the character
      Vector3 localTargetPosition = _coroutineRunner.transform.InverseTransformPoint(targetPosition);

      // Stop current natural movement
      StopCurrentEyeMovement();

      // Move to target
      _currentTargetPosition = localTargetPosition;
      _currentEyeMovementTween = Tween.LocalPosition(_controller, localTargetPosition, 0.5f, Ease.InOutSine);

      // After duration, return to natural movement
      _coroutineRunner.StartCoroutine(ReturnToNaturalMovementAfterDelay(duration));
    }

    /// <summary>
    /// Returns to natural eye movement after a delay
    /// </summary>
    private IEnumerator ReturnToNaturalMovementAfterDelay(float delay)
    {
      yield return new WaitForSeconds(delay);

      if (_isNaturalEyeMovementActive && _controller != null && _controller.gameObject.activeInHierarchy)
      {
        _currentTargetPosition = _originalLookAtPosition;
        StopCurrentEyeMovement();

        _currentEyeMovementTween = Tween.LocalPosition(_controller, _originalLookAtPosition, 0.5f, Ease.InOutSine);
      }
    }

    /// <summary>
    /// Follows a target transform and continues following during animation delays
    /// </summary>
    private IEnumerator FollowTargetCoroutine(Transform targetTransform, bool wasNaturalEyeMovementActive,
      float fadeDuration, float duration)
    {
      float startTime = Time.time;

      while (targetTransform != null && (duration <= 0f || Time.time - startTime < duration))
      {
        // Calculate target position in local space
        Vector3 localTarget = _coroutineRunner.transform.InverseTransformPoint(targetTransform.position);
        _currentTargetPosition = localTarget;

        // Move to target smoothly (only if distance is significant)
        float distance = Vector3.Distance(_controller.localPosition, localTarget);
        if (distance > POSITION_EPSILON && _controller.gameObject.activeInHierarchy)
        {
          StopCurrentEyeMovement();
          _currentEyeMovementTween = Tween.LocalPosition(_controller, localTarget, 0.1f, Ease.InOutSine);
        }

        // Wait a bit before updating position
        yield return new WaitForSeconds(0.1f);
      }

      // Duration reached or target is null, smooth transition back to natural eye movement
      _isFollowingTarget = false;
      _followTargetCoroutine = null;
      if (wasNaturalEyeMovementActive)
      {
        _currentTargetPosition = _originalLookAtPosition;
        StopCurrentEyeMovement();

        // Smooth transition back to original position using fadeDuration
        if (Vector3.Distance(_controller.localPosition, _originalLookAtPosition) > POSITION_EPSILON &&
            _controller.gameObject.activeInHierarchy)
        {
          _currentEyeMovementTween =
            Tween.LocalPosition(_controller, _originalLookAtPosition, fadeDuration, Ease.InOutSine);
        }

        StartNaturalEyeMovement();
      }
    }

    /// <summary>
    /// Sets the natural eye movement parameters
    /// </summary>
    public void SetNaturalEyeMovementSettings(bool enable, bool enableMicroSaccades = true,
      bool enableAttentionShifts = true,
      float microSaccadeRange = 0.025f, float microSaccadeFrequency = 0.25f,
      float attentionShiftChance = 0.7f, float attentionShiftRange = 0.25f, float attentionShiftInterval = 3f)
    {
      _settings.enableMicroSaccades = enableMicroSaccades;
      _settings.enableAttentionShifts = enableAttentionShifts;
      _settings.microSaccadeRange = microSaccadeRange;
      _settings.microSaccadeFrequency = microSaccadeFrequency;
      _settings.attentionShiftChance = attentionShiftChance;
      _settings.attentionShiftRange = attentionShiftRange;
      _settings.attentionShiftInterval = attentionShiftInterval;

      if (enable && !_isNaturalEyeMovementActive)
      {
        StartNaturalEyeMovement();
      }
      else if (!enable && _isNaturalEyeMovementActive)
      {
        StopNaturalEyeMovement();
      }
    }

    /// <summary>
    /// Updates the look at target reference
    /// </summary>
    public void UpdateLookAtTarget(Transform newLookAtTarget)
    {
      // Stop current movement if active
      if (_isNaturalEyeMovementActive)
      {
        StopNaturalEyeMovement();
      }

      _controller = newLookAtTarget;

      if (_controller != null)
      {
        _originalLookAtPosition = _controller.localPosition;
        _currentTargetPosition = _originalLookAtPosition;
      }

      // Restart if enabled
      if (_controller != null)
      {
        StartNaturalEyeMovement();
      }
    }

    /// <summary>
    /// Gets whether natural eye movement is currently active
    /// </summary>
    public bool IsNaturalEyeMovementActive => _isNaturalEyeMovementActive;

    /// <summary>
    /// Enables or disables micro-saccades
    /// </summary>
    public void SetMicroSaccadesEnabled(bool enabled)
    {
      _settings.enableMicroSaccades = enabled;
    }

    /// <summary>
    /// Enables or disables attention shifts
    /// </summary>
    public void SetAttentionShiftsEnabled(bool enabled)
    {
      _settings.enableAttentionShifts = enabled;
    }

    /// <summary>
    /// Sets the attention shift chance (0.0 to 1.0)
    /// </summary>
    public void SetAttentionShiftChance(float chance)
    {
      _settings.attentionShiftChance = Mathf.Clamp01(chance);
    }

    /// <summary>
    /// Gets the current settings
    /// </summary>
    public EyeMovementSettings GetSettings() => _settings;

    /// <summary>
    /// Handles animation played events
    /// </summary>
    private void OnAnimationPlayed(AnimationClip clip)
    {
      // Reset attention shift timer
      _lastAttentionShiftTime = Time.time;

      // Only reset position if not following a target
      if (_controller != null && !_isFollowingTarget)
      {
        StopCurrentEyeMovement();
        _currentTargetPosition = _originalLookAtPosition;

        // Only tween if the target position is different from current position
        if (Vector3.Distance(_controller.localPosition, _originalLookAtPosition) > POSITION_EPSILON &&
            _controller.gameObject.activeInHierarchy)
        {
          _currentEyeMovementTween = Tween.LocalPosition(_controller, _originalLookAtPosition, 0.2f, Ease.InOutSine);
        }
      }

      // Add delay based on animation duration
      _animationDelayEndTime = Time.time + clip.length;
    }

    #endregion

    #region IDisposable Implementation

    private bool _disposed = false;

    /// <summary>
    /// Disposes of the EyeMovement resources
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      System.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          // Stop target follow coroutine if any
          if (_followTargetCoroutine != null && _coroutineRunner != null)
          {
            _coroutineRunner.StopCoroutine(_followTargetCoroutine);
            _followTargetCoroutine = null;
          }

          // Stop natural eye movement
          StopNaturalEyeMovement();

          // Stop any active tween
          StopCurrentEyeMovement();

          // Unregister from animation events
          if (_locomotion != null)
          {
            _locomotion.OnAnimationPlayed -= OnAnimationPlayed;
          }

          // Clear references
          _controller = null;
          _coroutineRunner = null;
          _locomotion = null;
        }

        _disposed = true;
      }
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~EyeMovement()
    {
      Dispose(false);
    }

    #endregion
  }
}
