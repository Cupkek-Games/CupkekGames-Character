using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// How a <see cref="FaceOverlay"/> looks: each tint's colour and its place on the face, in
  /// eye gaps (<see cref="FaceFrame"/>), so one look fits every face of a style. Colours
  /// multiply the skin (white = no change); the tears' colour replaces it (above 1 lightens).
  /// </summary>
  [CreateAssetMenu(fileName = "FaceOverlayLook", menuName = "CupkekGames/Character/Face Overlay Look")]
  public class FaceOverlayLookSO : ScriptableObject
  {
    [SerializeField] private Shader _shader;

    [Header("Blush")]
    [SerializeField] private Color _blushColor = new(1f, 0.56f, 0.62f, 1f);

    [Tooltip("The blush's middle: x out from the face's middle, y down from the eyes, in eye gaps.")]
    [SerializeField] private Vector2 _cheek = new(0.58f, 0.4f);

    [Tooltip("The blush's half width and half height, in eye gaps.")]
    [SerializeField] private Vector2 _cheekSize = new(0.3f, 0.17f);

    [Tooltip("0 = a soft glow fading out, 1 = a crisp painted oval.")]
    [SerializeField, Range(0f, 1f)] private float _blushEdge;

    [Tooltip("The nose spot (the blushNose channel) at full weight, of the blush's colour.")]
    [SerializeField, Range(0f, 1f)] private float _noseStrength = 0.85f;

    [Tooltip("The nose spot: x down from the eyes, y and z its half width and half height, in eye gaps.")]
    [SerializeField] private Vector3 _nose = new(0.14f, 0.36f, 0.11f);

    [Header("Blush lines")]
    [SerializeField] private Color _linesColor = new(0.95f, 0.62f, 0.66f, 1f);
    [SerializeField, Min(0.01f)] private float _lineSpacing = 0.085f;
    [SerializeField, Min(0.001f)] private float _lineWidth = 0.022f;
    [Tooltip("The strokes' slant, in degrees.")]
    [SerializeField] private float _lineAngle = 30f;
    [Tooltip("The strokes' half length, of the blush's half height.")]
    [SerializeField, Min(0.1f)] private float _lineReach = 0.7f;

    [Header("Gloom")]
    [SerializeField] private Color _gloomColor = new(0.62f, 0.58f, 0.86f, 1f);
    [Tooltip("x where the gloom starts, y where it is full, up from the eyes in eye gaps.")]
    [SerializeField] private Vector2 _gloom = new(-0.3f, 0.45f);

    [Header("Tears")]
    [SerializeField, ColorUsage(false, true)] private Color _tearsColor = new(1.2f, 1.45f, 1.75f, 1f);
    [Tooltip("x out from the face's middle, y the top under the eye, z the bottom, w the streak's width, in eye gaps.")]
    [SerializeField] private Vector4 _tear = new(0.5f, -0.32f, -1.1f, 0.07f);

    [Tooltip("How far behind the eyes, in eye gaps, the face still tints; the back of the head never does.")]
    [SerializeField, Min(0.1f)] private float _depth = 0.9f;

    public Shader Shader => _shader;

    /// <summary>The nose spot at full weight (0 to 1).</summary>
    public float NoseStrength => _noseStrength;

    private static readonly int BlushColorId = UnityEngine.Shader.PropertyToID("_BlushColor");
    private static readonly int LinesColorId = UnityEngine.Shader.PropertyToID("_LinesColor");
    private static readonly int GloomColorId = UnityEngine.Shader.PropertyToID("_GloomColor");
    private static readonly int TearsColorId = UnityEngine.Shader.PropertyToID("_TearsColor");
    private static readonly int CheekId = UnityEngine.Shader.PropertyToID("_Cheek");
    private static readonly int BlushEdgeId = UnityEngine.Shader.PropertyToID("_BlushEdge");
    private static readonly int LinesId = UnityEngine.Shader.PropertyToID("_Lines");
    private static readonly int NoseId = UnityEngine.Shader.PropertyToID("_Nose");
    private static readonly int GloomId = UnityEngine.Shader.PropertyToID("_Gloom");
    private static readonly int TearId = UnityEngine.Shader.PropertyToID("_Tear");
    private static readonly int DepthId = UnityEngine.Shader.PropertyToID("_Depth");

    /// <summary>Writes the look into <paramref name="block"/>.</summary>
    public void Apply(MaterialPropertyBlock block)
    {
      block.SetColor(BlushColorId, _blushColor);
      block.SetColor(LinesColorId, _linesColor);
      block.SetColor(GloomColorId, _gloomColor);
      block.SetColor(TearsColorId, _tearsColor);
      block.SetVector(CheekId, new Vector4(_cheek.x, _cheek.y, _cheekSize.x, _cheekSize.y));
      block.SetFloat(BlushEdgeId, _blushEdge);
      block.SetVector(LinesId, new Vector4(_lineSpacing, _lineWidth, _lineAngle, _lineReach));
      block.SetVector(NoseId, new Vector4(0f, _nose.x, _nose.y, _nose.z));
      block.SetVector(GloomId, _gloom);
      block.SetVector(TearId, _tear);
      block.SetFloat(DepthId, _depth);
    }

    private void Reset()
    {
      _shader = UnityEngine.Shader.Find("CupkekGames/Character/FaceOverlay");
    }
  }
}
