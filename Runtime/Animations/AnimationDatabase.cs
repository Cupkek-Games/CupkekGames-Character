using CupkekGames.Animations;
using UnityEngine;
using CupkekGames.KeyValueDatabases;

namespace CupkekGames.Character
{
  public class AnimationDatabase : MonoBehaviour
  {
    [SerializeField] private KeyValueDatabase<AnimationDatabaseClipType, AnimationClipData> _database;

    public AnimationClipData GetAnimation(AnimationDatabaseClipType animationDatabaseClipType)
    {
      if (_database.TryGetValue(animationDatabaseClipType, out AnimationClipData clipData))
      {
        return clipData;
      }

      Debug.LogError($"Animation not found for {animationDatabaseClipType}");

      return null;
    }
  }
}