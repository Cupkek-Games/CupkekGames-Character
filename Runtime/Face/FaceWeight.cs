using System;
using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>One face channel (a blendshape name) and its weight, 0..100.</summary>
  [Serializable]
  public struct FaceWeight
  {
    public string Name;
    [Range(0, 100)] public float Weight;

    public FaceWeight(string name, float weight)
    {
      Name = name;
      Weight = weight;
    }
  }

  /// <summary>Whether the blinker may close the eyes while an expression shows.</summary>
  public enum FaceBlinkPolicy
  {
    Allow,

    /// <summary>For faces whose eyes are already shaped closed (happy ^ ^ eyes, a wink).</summary>
    Suppress,
  }
}
