using UnityEngine;
using CupkekGames.KeyValueDatabases;

namespace CupkekGames.Character
{
  public class AnimatorControllerLayerDatabase : MonoBehaviour
  {
    [SerializeField] private KeyValueDatabase<AnimatorControllerLayer, AvatarMask> _masks;

    public AvatarMask GetMask(AnimatorControllerLayer layer)
    {
      if (_masks.TryGetValue(layer, out AvatarMask mask))
      {
        return mask;
      }

      return null;
    }
  }
}