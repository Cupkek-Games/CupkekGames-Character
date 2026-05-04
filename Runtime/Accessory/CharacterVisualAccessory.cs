using UnityEngine;
using System;


namespace CupkekGames.Character
{
  [Serializable]
  public class CharacterVisualAccessory
  {
    [SerializeField] public string Role = VisualAccessoryRoleKinds.DEFAULT;
    [SerializeField] public GameObject GameObject;
  }
}
