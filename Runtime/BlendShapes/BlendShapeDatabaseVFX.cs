
using CupkekGames.VFX;
using CupkekGames.KeyValueDatabases;

namespace CupkekGames.Character
{
  public class BlendShapeDatabaseVFX : KeyValueDatabaseMono<BlendShapeEnum, VFXBundle>
  {
    private void Awake()
    {
      foreach (var item in Values)
      {
        item.Prewarm(gameObject);
      }
    }
  }
}
