using UnityEngine;
using System;


namespace CupkekGames.Character
{
  [Serializable]
  public class CharacterVisualAccessory
  {
    [SerializeField] public VisualAccessoryRole Role;
    [SerializeField] public GameObject GameObject;
  }
}