using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  [RequireComponent(typeof(BlendShapeController))]
  public class AutoExpression : MonoBehaviour
  {
    private BlendShapeController _controller;
    Coroutine m_coroutine;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private float _velocity = 1.0f;
    [SerializeField] private float _waitBeforeNext = 0.5f;
    [SerializeField] private List<string> BlendShapes;
    [SerializeField] private List<int> MaxValues;

    IEnumerator RoutineNest(string name, float velocity, float wait, float maxValue)
    {
      for (var value = 0.0f; value <= maxValue; value += velocity)
      {
        _controller.SetWeight(name, value);
        yield return null;
      }
      _controller.SetWeight(name, maxValue);
      yield return new WaitForSeconds(wait);
      for (var value = maxValue; value >= 0; value -= velocity)
      {
        _controller.SetWeight(name, value);
        yield return null;
      }
      _controller.SetWeight(name, 0);
      yield return new WaitForSeconds(_waitBeforeNext);
    }

    IEnumerator Routine()
    {
      while (true)
      {
        yield return new WaitForSeconds(1.0f);

        for (var i = 0; i < BlendShapes.Count; i++)
        {
          yield return RoutineNest(BlendShapes[i], _velocity, _duration, MaxValues[i]);
        }
      }
    }

    private void OnEnable()
    {
      _controller = GetComponent<BlendShapeController>();
      m_coroutine = StartCoroutine(Routine());
    }

    private void OnDisable()
    {
      StopCoroutine(m_coroutine);
    }
  }
}
