using System;
using System.Collections.Generic;
using CupkekGames.Data;
using CupkekGames.Services;
using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>Expression key (Neutral, Joy, …) → <see cref="FaceExpressionSO"/>.</summary>
  [CreateAssetMenu(fileName = "FaceExpressionCatalog", menuName = "CupkekGames/Character/Face Expression Catalog")]
  public class FaceExpressionCatalog : AssetCatalog<FaceExpressionSO>
  {
  }

  /// <summary>
  /// Resolves expression keys across every registered <see cref="FaceExpressionCatalog"/>,
  /// whatever catalog id each one was authored with.
  /// </summary>
  public static class FaceExpressions
  {
    public static bool TryGet(string key, out FaceExpressionSO expression)
    {
      expression = null;
      if (string.IsNullOrEmpty(key)) return false;

      IReadOnlyList<IAssetCatalog<FaceExpressionSO>> catalogs = ServiceLocator.GetAll<IAssetCatalog<FaceExpressionSO>>();
      for (int i = 0; i < catalogs.Count; i++)
      {
        if (catalogs[i] != null && catalogs[i].TryGetValue(key, out UnityEngine.Object value) &&
            value is FaceExpressionSO typed)
        {
          expression = typed;
          return true;
        }
      }

      return false;
    }

    /// <summary>Throws when no registered catalog has <paramref name="key"/>.</summary>
    public static FaceExpressionSO GetRequired(string key)
    {
      if (TryGet(key, out FaceExpressionSO expression)) return expression;

      throw new InvalidOperationException(
        $"No FaceExpressionCatalog has an expression for key '{key}'. " +
        "Is the key a typo, or is the catalog asset missing from the service registry?");
    }

    public static IEnumerable<string> Keys
    {
      get
      {
        IReadOnlyList<IAssetCatalog<FaceExpressionSO>> catalogs = ServiceLocator.GetAll<IAssetCatalog<FaceExpressionSO>>();
        for (int i = 0; i < catalogs.Count; i++)
        {
          if (catalogs[i] == null) continue;
          foreach (string key in catalogs[i].GetKeys())
            yield return key;
        }
      }
    }
  }
}
