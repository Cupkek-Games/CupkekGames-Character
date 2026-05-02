using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  [CreateAssetMenu(fileName = "BlendShapeList", menuName = "3DModel/BlendShapeList")]
  public class BlendShapeListSO : ScriptableObject
  {
    public List<BlendShapeData> BlendShapes;

  }
}