using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CupkekGames.Character
{
  public class BlendShapeController : MonoBehaviour
  {
    public event Action<string> OnPreExpressionPlay;

    [SerializeField] private BlendShapeListSO _targetSO;

    public BlendShapeListSO TargetSO
    {
      get { return _targetSO; }
      set
      {
        _targetSO = value;
        TargetBlendShapes = _targetSO.BlendShapes;
      }
    }

    public void SetTargetSO(BlendShapeListSO targetSO, string expression = null)
    {
      if (!string.IsNullOrEmpty(expression))
      {
        OnPreExpressionPlay?.Invoke(expression);
      }

      TargetSO = targetSO;
    }

    public List<BlendShapeData> TargetBlendShapes;
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
    [SerializeField] private BlendShapeListBlend _currentBlend;
    private Coroutine _delayedBlendCoroutine;

    public void OnEnable()
    {
      TargetBlendShapes = _targetSO?.BlendShapes;
      if (TargetBlendShapes == null || TargetBlendShapes.Count == 0)
      {
        return;
      }

      ApplyToMesh(TargetBlendShapes);
    }

    public void OnDisable()
    {
      if (_currentBlend != null)
      {
        _currentBlend.Stop();
      }
    }

    public void ApplyToMesh(List<BlendShapeData> blendShapes)
    {
      if (_skinnedMeshRenderer == null)
      {
        return;
      }

      foreach (var blendShape in blendShapes)
      {
        var index = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShape.Name);
        if (index < 0)
        {
          Debug.LogError("BlendShape not found: " + blendShape.Name);
          continue;
        }

        _skinnedMeshRenderer.SetBlendShapeWeight(index, blendShape.Weight);
      }
    }

    public void SetWeight(string name, float weight)
    {
      if (_skinnedMeshRenderer == null)
      {
        return;
      }

      var index = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(name);
      if (index < 0)
      {
        Debug.LogError("BlendShape not found: " + name);
        return;
      }

      _skinnedMeshRenderer.SetBlendShapeWeight(index, weight);
    }

    public float GetWeight(string name)
    {
      if (_skinnedMeshRenderer == null)
      {
        return 0;
      }

      var index = _skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(name);
      if (index < 0)
      {
        Debug.LogError("BlendShape not found: " + name);
        return 0;
      }

      return _skinnedMeshRenderer.GetBlendShapeWeight(index);
    }

    public void ApplyValuesFromTarget()
    {
      ApplyToMesh(TargetBlendShapes);
    }

    public void BlendToTarget(float duration)
    {
      if (_currentBlend != null)
      {
        _currentBlend.Stop();
      }

      _currentBlend = new BlendShapeListBlend(TargetBlendShapes, duration);
      _currentBlend.Play(_skinnedMeshRenderer);
    }

    public void BlendToNewTargetWithDelay(BlendShapeListSO target, float duration, float delay,
      string expression = null)
    {
      if (_delayedBlendCoroutine != null)
      {
        StopCoroutine(_delayedBlendCoroutine);
      }

      _delayedBlendCoroutine = StartCoroutine(DelayedBlendToTarget(target, duration, delay, expression));
    }

    private IEnumerator DelayedBlendToTarget(BlendShapeListSO target, float duration, float delay,
      string expression)
    {
      yield return new WaitForSeconds(delay);

      if (!string.IsNullOrEmpty(expression))
      {
        OnPreExpressionPlay?.Invoke(expression);
      }

      _targetSO = target;
      TargetBlendShapes = _targetSO.BlendShapes;
      BlendToTarget(duration);
    }
  }
}
