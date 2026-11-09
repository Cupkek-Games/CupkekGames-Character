using System.Collections.Generic;
using CupkekGames.Animations;
using UnityEngine;
using CupkekGames.KeyValueDatabases;

namespace CupkekGames.Character
{
  public class AnimationDatabase : MonoBehaviour
  {
    [SerializeField] private KeyValueDatabase<string, AnimationClipData> _database;

    /// <summary>The kinds it holds, in the database's order.</summary>
    public IReadOnlyCollection<string> Kinds => _database.Keys;

    public AnimationClipData GetAnimation(string kind)
    {
      if (string.IsNullOrEmpty(kind))
      {
        return null;
      }

      if (_database.TryGetValue(kind, out AnimationClipData clipData))
      {
        return clipData;
      }

      Debug.LogError($"Animation not found for {kind}");
      return null;
    }
  }
}
