using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  [System.Serializable]
  public struct BlendShapeData
  {
    public string Name;
    [Range(0, 100)]
    public float Weight;
  }
}