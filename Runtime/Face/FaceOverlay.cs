using UnityEngine;
using UnityEngine.Rendering;

namespace CupkekGames.Character
{
  /// <summary>
  /// The face's tints as face channels (<see cref="FaceController.AddChannel"/>): a blush,
  /// a blush on the nose, the blush's hatch lines, gloom over the upper face and tears, each
  /// 0 to 100. Expressions list them
  /// like any shape key (Shy: blush 70; Flustered: blush 100, blushLines 100), so they fade
  /// and time with the face. Drawn as a second pass over the skin: a copy of the skin's
  /// renderer on the same mesh and bones (made at enable, never saved) with the
  /// FaceOverlay shader, placed in the face's own space (<see cref="FaceFrame"/>), so it needs
  /// no mask and fits any face with eye bones. The copy draws only while a tint shows.
  /// Works in edit mode, so a portrait booth shoots it.
  /// </summary>
  [ExecuteAlways]
  [DisallowMultipleComponent]
  [RequireComponent(typeof(FaceController))]
  public class FaceOverlay : MonoBehaviour
  {
    public const string Blush = "blush";
    public const string BlushNose = "blushNose";
    public const string BlushLines = "blushLines";
    public const string Gloom = "gloom";
    public const string Tears = "tears";

    [SerializeField] private FaceOverlayLookSO _look;

    [Tooltip("The skin's renderer. Empty = the face renderer that carries the first blink channel.")]
    [SerializeField] private SkinnedMeshRenderer _skin;

    [Tooltip("The skin's submesh on that renderer.")]
    [SerializeField, Min(0)] private int _skinSubmesh;

    private static readonly int FaceOriginId = Shader.PropertyToID("_FaceOrigin");
    private static readonly int FaceRightId = Shader.PropertyToID("_FaceRight");
    private static readonly int FaceUpId = Shader.PropertyToID("_FaceUp");
    private static readonly int FaceForwardId = Shader.PropertyToID("_FaceForward");
    private static readonly int AmountsId = Shader.PropertyToID("_Amounts");
    private static readonly int NoseAmountId = Shader.PropertyToID("_NoseAmount");
    private static readonly int SkipId = Shader.PropertyToID("_Skip");

    private FaceController _face;
    private FaceFrame _frame;
    private SkinnedMeshRenderer _copy;
    private Material _material;
    private Material _skip;
    private MaterialPropertyBlock _block;
    private Vector4 _amounts;
    private float _nose;

    private System.Action<float> _writeBlush;
    private System.Action<float> _writeLines;
    private System.Action<float> _writeGloom;
    private System.Action<float> _writeTears;
    private System.Action<float> _writeNose;

    public FaceOverlayLookSO Look => _look;

    /// <summary>The tints now, 0 to 1: x blush, y blush lines, z gloom, w tears.</summary>
    public Vector4 Amounts => _amounts;

    private void OnEnable()
    {
      _face = GetComponent<FaceController>();
      if (_look == null || _look.Shader == null)
      {
        Debug.LogError($"[FaceOverlay] {name} has no look (or its look has no shader).", this);
        return;
      }

      _frame = FaceFrame.Of(transform);
      if (_frame == null)
      {
        Debug.LogError($"[FaceOverlay] {name} has no humanoid head with eye bones to place the tints on.", this);
        return;
      }

      if (_skin == null) _skin = FindSkin();
      if (_skin == null || _skin.sharedMesh == null || _skinSubmesh >= _skin.sharedMesh.subMeshCount)
      {
        Debug.LogError($"[FaceOverlay] {name}: no skin renderer with submesh {_skinSubmesh}.", this);
        return;
      }

      MakeCopy();
      _writeBlush = weight => Set(ref _amounts.x, weight);
      _writeLines = weight => Set(ref _amounts.y, weight);
      _writeGloom = weight => Set(ref _amounts.z, weight);
      _writeTears = weight => Set(ref _amounts.w, weight);
      _writeNose = weight => Set(ref _nose, weight);
      _face.AddChannel(Blush, _writeBlush);
      _face.AddChannel(BlushLines, _writeLines);
      _face.AddChannel(Gloom, _writeGloom);
      _face.AddChannel(Tears, _writeTears);
      _face.AddChannel(BlushNose, _writeNose);
      RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    }

    private void OnDisable()
    {
      RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
      if (_writeBlush != null)
      {
        _face.RemoveChannel(Blush, _writeBlush);
        _face.RemoveChannel(BlushLines, _writeLines);
        _face.RemoveChannel(Gloom, _writeGloom);
        _face.RemoveChannel(Tears, _writeTears);
        _face.RemoveChannel(BlushNose, _writeNose);
        _writeBlush = null;
      }

      Kill(_copy != null ? _copy.gameObject : null);
      Kill(_material);
      Kill(_skip);
      _copy = null;
      _material = null;
      _skip = null;
      _amounts = Vector4.zero;
      _nose = 0f;
    }

    // The renderer that carries the first blink channel: the face.
    private SkinnedMeshRenderer FindSkin()
    {
      string blink = _face.BlinkChannels.Count > 0 ? _face.BlinkChannels[0] : null;
      foreach (SkinnedMeshRenderer renderer in _face.CollectRenderers())
      {
        if (blink == null || renderer.sharedMesh.GetBlendShapeIndex(blink) >= 0) return renderer;
      }

      return null;
    }

    private void MakeCopy()
    {
      var copy = new GameObject($"{_skin.name} (face overlay)") { hideFlags = HideFlags.HideAndDontSave, layer = _skin.gameObject.layer };
      copy.transform.SetParent(_skin.transform.parent, false);
      copy.transform.localPosition = _skin.transform.localPosition;
      copy.transform.localRotation = _skin.transform.localRotation;
      copy.transform.localScale = _skin.transform.localScale;

      _copy = copy.AddComponent<SkinnedMeshRenderer>();
      _copy.sharedMesh = _skin.sharedMesh;
      _copy.bones = _skin.bones;
      _copy.rootBone = _skin.rootBone;
      _copy.localBounds = _skin.localBounds;
      _copy.updateWhenOffscreen = _skin.updateWhenOffscreen;
      _copy.renderingLayerMask = _skin.renderingLayerMask;
      _copy.shadowCastingMode = ShadowCastingMode.Off;
      _copy.receiveShadows = false;
      _copy.skinnedMotionVectors = false;
      _copy.lightProbeUsage = LightProbeUsage.Off;
      _copy.reflectionProbeUsage = ReflectionProbeUsage.Off;

      _material = new Material(_look.Shader) { name = "FaceOverlay", hideFlags = HideFlags.HideAndDontSave };
      var materials = new Material[_skinSubmesh + 1];
      if (_skinSubmesh > 0)
      {
        _skip = new Material(_look.Shader) { name = "FaceOverlay (skip)", hideFlags = HideFlags.HideAndDontSave };
        _skip.SetFloat(SkipId, 1f);
        for (int i = 0; i < _skinSubmesh; i++) materials[i] = _skip;
      }

      materials[_skinSubmesh] = _material;
      _copy.sharedMaterials = materials;
      _copy.enabled = false;
      _block = new MaterialPropertyBlock();
    }

    private void Set(ref float amount, float weight)
    {
      amount = Mathf.Clamp01(weight / 100f);
      if (_copy != null) _copy.enabled = Showing;
    }

    private bool Showing => _amounts.x > 0.001f || _amounts.y > 0.001f || _amounts.z > 0.001f || _amounts.w > 0.001f || _nose > 0.001f;

    private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
      if (_copy == null) return;
      bool show = Showing && _skin != null && _skin.enabled && _skin.gameObject.activeInHierarchy;
      _copy.enabled = show;
      if (!show) return;

      Mesh mesh = _skin.sharedMesh;
      for (int i = 0; i < mesh.blendShapeCount; i++) _copy.SetBlendShapeWeight(i, _skin.GetBlendShapeWeight(i));

      _frame.Refresh();
      _copy.GetPropertyBlock(_block, _skinSubmesh);
      _look.Apply(_block);
      _block.SetVector(FaceOriginId, new Vector4(_frame.Origin.x, _frame.Origin.y, _frame.Origin.z, _frame.Gap));
      _block.SetVector(FaceRightId, _frame.Right);
      _block.SetVector(FaceUpId, _frame.Up);
      _block.SetVector(FaceForwardId, _frame.Forward);
      _block.SetVector(AmountsId, _amounts);
      _block.SetFloat(NoseAmountId, _nose * _look.NoseStrength);
      _copy.SetPropertyBlock(_block, _skinSubmesh);
    }

    private static void Kill(Object thing)
    {
      if (thing == null) return;
      if (Application.isPlaying) Destroy(thing);
      else DestroyImmediate(thing);
    }
  }
}
