using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  [RequireComponent(typeof(BlendShapeController))]
  public class Blinker : MonoBehaviour
  {
    [System.Serializable]
    public class BlinkerSettings
    {
      [Header("Blend Shape Settings")]
      public List<string> BlendShapeNames = new List<string> { "Fcl_EYE_Close", "Fcl_EYE_Iris_Hide" };

      public float MaxWeight = 80f;

      public List<string> IgnoreBlendShapes = new List<string>()
        { "Fcl_ALL_Angry", "Fcl_ALL_Joy", "Fcl_ALL_Sorrow", "Fcl_ALL_Surprised" };

      [Header("Timing Settings")] public float MinInterval = 4.0f;
      public float MaxInterval = 8.0f;
      public float ClosingTime = 0.06f;
      public float OpeningSeconds = 0.03f;
      public float CloseSeconds = 0.1f;
    }

    private BlinkerSettings _settings = new BlinkerSettings();

    private BlendShapeController _controller;
    private Coroutine _coroutine;
    private float _nextRequest;
    private bool _request;
    private bool _isEnabled = true;

    public bool Request
    {
      get { return _request; }
      set
      {
        if (Time.time < _nextRequest)
        {
          return;
        }

        _request = value;
        _nextRequest = Time.time + 1.0f;
      }
    }

    IEnumerator BlinkRoutine()
    {
      while (true)
      {
        // Don't blink if disabled (non-neutral expression active)
        if (!_isEnabled)
        {
          yield return null;
          continue;
        }

        var waitTime = Time.time + Random.Range(_settings.MinInterval, _settings.MaxInterval);
        // Wait
        while (waitTime > Time.time)
        {
          // Cancel wait if request or disabled
          if (Request || !_isEnabled)
          {
            _request = false;
            break;
          }

          yield return null;
        }

        // Check again if disabled before starting blink
        if (!_isEnabled)
        {
          continue;
        }

        // Cancel if ignoreBlendShapes are not 0
        var ignore = false;
        foreach (var ignoreBlendShape in _settings.IgnoreBlendShapes)
        {
          if (_controller.GetWeight(ignoreBlendShape) > 0)
          {
            ignore = true;
            break;
          }
        }

        if (ignore)
        {
          continue;
        }

        // close
        var value = 0.0f;
        var closeSpeed = _settings.MaxWeight / _settings.CloseSeconds;
        while (true)
        {
          // Stop immediately if disabled during blink
          if (!_isEnabled)
          {
            // Reset blend shapes to 0 when interrupted
            foreach (string blendShapeName in _settings.BlendShapeNames)
            {
              _controller.SetWeight(blendShapeName, 0);
            }

            break;
          }

          value += Time.deltaTime * closeSpeed;
          if (value >= _settings.MaxWeight)
          {
            break;
          }

          foreach (string blendShapeName in _settings.BlendShapeNames)
          {
            _controller.SetWeight(blendShapeName, value);
          }

          yield return null;
        }

        // Check again before continuing
        if (!_isEnabled)
        {
          continue;
        }

        foreach (string blendShapeName in _settings.BlendShapeNames)
        {
          _controller.SetWeight(blendShapeName, _settings.MaxWeight);
        }

        // wait...
        yield return new WaitForSeconds(_settings.ClosingTime);

        // Check again before opening
        if (!_isEnabled)
        {
          // Reset blend shapes to 0 when interrupted
          foreach (string blendShapeName in _settings.BlendShapeNames)
          {
            _controller.SetWeight(blendShapeName, 0);
          }

          continue;
        }

        // open
        value = _settings.MaxWeight;
        var openSpeed = _settings.MaxWeight / _settings.OpeningSeconds;
        while (true)
        {
          // Stop immediately if disabled during opening
          if (!_isEnabled)
          {
            // Reset blend shapes to 0 when interrupted
            foreach (string blendShapeName in _settings.BlendShapeNames)
            {
              _controller.SetWeight(blendShapeName, 0);
            }

            break;
          }

          value -= Time.deltaTime * openSpeed;
          if (value < 0)
          {
            break;
          }

          foreach (string blendShapeName in _settings.BlendShapeNames)
          {
            _controller.SetWeight(blendShapeName, value);
          }

          yield return null;
        }

        foreach (string blendShapeName in _settings.BlendShapeNames)
        {
          _controller.SetWeight(blendShapeName, 0);
        }
      }
    }

    private void OnEnable()
    {
      _controller = GetComponent<BlendShapeController>();

      // Subscribe to BlendShapeController's pre-expression event
      if (_controller != null)
      {
        _controller.OnPreExpressionPlay += OnExpressionPlay;
      }

      Start();
    }

    private void OnDisable()
    {
      if (_controller != null)
      {
        _controller.OnPreExpressionPlay -= OnExpressionPlay;
      }

      Stop();
    }

    private void OnExpressionPlay(string expression)
    {
      if (expression == BlendShapeKinds.Neutral)
      {
        // Enable blinker for neutral expression
        _isEnabled = true;
      }
      else
      {
        // Disable blinker for non-neutral expressions
        _isEnabled = false;
        // Stop immediately - this will interrupt any ongoing blink
        Stop();
        // Reset eye blend shapes to 0 to prevent conflicts
        foreach (string blendShapeName in _settings.BlendShapeNames)
        {
          _controller.SetWeight(blendShapeName, 0);
        }

        // Restart the coroutine so it can resume when enabled again
        Start();
      }
    }

    public void Start()
    {
      _coroutine = StartCoroutine(BlinkRoutine());
    }

    public void Stop()
    {
      if (_coroutine != null)
      {
        StopCoroutine(_coroutine);
        _coroutine = null;
      }
    }

    /// <summary>
    /// Gets the current blinker settings
    /// </summary>
    public BlinkerSettings GetSettings() => _settings;

    /// <summary>
    /// Sets the blinker settings
    /// </summary>
    public void SetSettings(BlinkerSettings settings)
    {
      if (settings != null)
      {
        _settings = settings;
      }
    }

    /// <summary>
    /// Sets individual blinker parameters
    /// </summary>
    public void SetBlinkerSettings(List<string> blendShapeNames = null, float maxWeight = -1f,
      List<string> ignoreBlendShapes = null, float minInterval = -1f, float maxInterval = -1f,
      float closingTime = -1f, float openingSeconds = -1f, float closeSeconds = -1f)
    {
      if (blendShapeNames != null)
        _settings.BlendShapeNames = blendShapeNames;
      if (maxWeight >= 0)
        _settings.MaxWeight = maxWeight;
      if (ignoreBlendShapes != null)
        _settings.IgnoreBlendShapes = ignoreBlendShapes;
      if (minInterval >= 0)
        _settings.MinInterval = minInterval;
      if (maxInterval >= 0)
        _settings.MaxInterval = maxInterval;
      if (closingTime >= 0)
        _settings.ClosingTime = closingTime;
      if (openingSeconds >= 0)
        _settings.OpeningSeconds = openingSeconds;
      if (closeSeconds >= 0)
        _settings.CloseSeconds = closeSeconds;
    }
  }
}
