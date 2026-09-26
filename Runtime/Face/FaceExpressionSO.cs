using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// One facial expression as an absolute state. Only the channels that move are listed:
  /// every channel not listed is 0 while this expression shows, so switching faces never
  /// leaves residue. Channels a character's meshes don't have are skipped.
  /// </summary>
  [CreateAssetMenu(fileName = "FaceExpression", menuName = "CupkekGames/Character/Face Expression")]
  public class FaceExpressionSO : ScriptableObject
  {
    [SerializeField] private List<FaceWeight> _weights = new();
    [SerializeField] private FaceBlinkPolicy _blink = FaceBlinkPolicy.Allow;
    [Tooltip("Crossfade time into this expression when the caller doesn't give one.")]
    [SerializeField, Min(0f)] private float _blendSeconds = 0.25f;

    public IReadOnlyList<FaceWeight> Weights => _weights;
    public FaceBlinkPolicy Blink => _blink;
    public float BlendSeconds => _blendSeconds;

    internal void Configure(IEnumerable<FaceWeight> weights, FaceBlinkPolicy blink, float blendSeconds)
    {
      _weights = new List<FaceWeight>(weights);
      _blink = blink;
      _blendSeconds = blendSeconds;
    }

    private void OnValidate()
    {
      var seen = new HashSet<string>();
      foreach (FaceWeight weight in _weights)
      {
        if (string.IsNullOrEmpty(weight.Name))
        {
          Debug.LogWarning($"[FaceExpression] '{name}' has an entry with no channel name.", this);
        }
        else if (!seen.Add(weight.Name))
        {
          Debug.LogWarning($"[FaceExpression] '{name}' lists '{weight.Name}' twice; the last entry wins.", this);
        }
      }
    }
  }
}
