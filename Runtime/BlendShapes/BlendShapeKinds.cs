namespace CupkekGames.Character
{
  /// <summary>
  /// Reserved string-kind constants for blend-shape expressions. Used as keys in
  /// <see cref="BlendShapeDatabase"/> / <see cref="BlendShapeDatabaseVFX"/>. Games may add
  /// their own kinds — these are the package-level defaults.
  /// </summary>
  public static class BlendShapeKinds
  {
    public const string Neutral = "Neutral";
    public const string Angry = "Angry";
    public const string Fun = "Fun";
    public const string Joy = "Joy";
    public const string Sorrow = "Sorrow";
    public const string Surprised = "Surprised";
    public const string Smug = "Smug";
    public const string Love = "Love";
    public const string HeartBreak = "HeartBreak";
  }
}
