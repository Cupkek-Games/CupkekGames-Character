using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

namespace CupkekGames.Character
{
  /// <summary>
  /// Blend shape list blend.
  /// !!!!!!!!!
  /// BlendShapeLists must be equal in size and order!!!
  /// !!!!!!!!!
  /// </summary>
  public class BlendShapeListBlend
  {
    // Properties
    private readonly List<BlendShapeData> _target;
    private readonly float _duration;

    // State
    private readonly List<Tween> _tweens = new List<Tween>();

    public BlendShapeListBlend(List<BlendShapeData> target, float duration)
    {
      _target = target;
      _duration = duration;
    }

    public void Play(SkinnedMeshRenderer skinnedMeshRenderer)
    {
      Stop();

      for (int i = 0; i < _target.Count; i++)
      {
        var blendShape = _target[i];
        var index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShape.Name);
        if (index < 0)
        {
          Debug.LogError("BlendShape not found in skkined mesh: " + blendShape.Name);
          continue;
        }

        Tween tween = Tween.Custom(skinnedMeshRenderer.GetBlendShapeWeight(index), _target[index].Weight, _duration, onValueChange: x => skinnedMeshRenderer.SetBlendShapeWeight(index, x));

        _tweens.Add(tween);
      }
    }

    public void Stop()
    {
      foreach (var tween in _tweens)
      {
        tween.Stop();
      }
      _tweens.Clear();
    }
  }
}