using UnityEngine;

using CupkekGames.VFX;
using CupkekGames.KeyValueDatabases;

namespace CupkekGames.Character
{
  public class BlendShapeDatabase : KeyValueDatabaseMonoSO<string, BlendShapeListSO>
  {
    [SerializeField] private BlendShapeDatabaseVFX vfx;

    public BlendShapeListSO GetByType(string kind)
    {
      return GetValue(kind);
    }

    public VFXBundle GetVFX(string kind)
    {
      return vfx.GetValue(kind);
    }
  }
}
