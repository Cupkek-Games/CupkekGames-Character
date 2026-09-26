using CupkekGames.VFX;
using CupkekGames.KeyValueDatabases;

namespace CupkekGames.Character
{
  /// <summary>
  /// Expression key → emote VFX (the heart over the head for Love, and so on). Lives on a
  /// persistent object because each <see cref="VFXBundle"/> owns a pool parented to it.
  /// Optional: no database, or no entry for a key, means the expression plays without an emote.
  /// </summary>
  public class EmoteVFXDatabase : KeyValueDatabaseMono<string, VFXBundle>
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
