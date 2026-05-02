using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  [RequireComponent(typeof(BlendShapeController))]
  public class AutoExpressionList : MonoBehaviour
  {
    private BlendShapeController _controller;
    Coroutine m_coroutine;

    [SerializeField] float m_wait = 0.5f;
    [SerializeField] private BlendShapeListSO[] BlendShapeLists;

    IEnumerator RoutineNest(BlendShapeListSO next, float wait)
    {
      _controller.TargetSO = next;
      _controller.BlendToTarget(wait);
      yield return new WaitForSeconds(wait * 2);
    }

    IEnumerator Routine()
    {
      while (true)
      {
        yield return new WaitForSeconds(1.0f);

        for (var i = 0; i < BlendShapeLists.Length; i++)
        {
          yield return RoutineNest(BlendShapeLists[i], m_wait);
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
