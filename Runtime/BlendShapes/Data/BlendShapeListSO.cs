using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  [CreateAssetMenu(fileName = "BlendShapeList", menuName = "CupkekGames/Character/Blend Shape List")]
  public class BlendShapeListSO : ScriptableObject
  {
    public List<BlendShapeData> BlendShapes;

  }
}