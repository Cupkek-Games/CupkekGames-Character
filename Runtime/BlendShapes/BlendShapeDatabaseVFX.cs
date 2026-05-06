using CupkekGames.VFX;
using CupkekGames.KeyValueDatabases;

namespace CupkekGames.Character
{
  public class BlendShapeDatabaseVFX : KeyValueDatabaseMono<string, VFXBundle>
  {
    protected override void Awake()
    {
      base.Awake();
      foreach (var item in Values)
      {
        item.Prewarm(gameObject);
      }
    }
  }
}
