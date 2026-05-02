using UnityEngine;

using CupkekGames.VFX;
using CupkekGames.KeyValueDatabases;

namespace CupkekGames.Character
{
  public class BlendShapeDatabase : KeyValueDatabaseMonoSO<BlendShapeEnum, BlendShapeListSO>
  {
    [SerializeField] private BlendShapeDatabaseVFX vfx;

    public BlendShapeListSO GetByType(BlendShapeEnum type)
    {
      return GetValue(type);
    }

    public VFXBundle GetVFX(BlendShapeEnum type)
    {
      return vfx.GetValue(type);
    }
  }
}
