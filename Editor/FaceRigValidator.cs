using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character.Editor
{
  public enum FaceRigIssueSeverity
  {
    Warning,
    Error,
  }

  public readonly struct FaceRigIssue
  {
    public readonly FaceRigIssueSeverity Severity;
    public readonly string Message;
    public readonly Object Context;

    public FaceRigIssue(FaceRigIssueSeverity severity, string message, Object context)
    {
      Severity = severity;
      Message = message;
      Context = context;
    }

    public override string ToString() => $"[{Severity}] {Message}";
  }

  /// <summary>
  /// Checks a character (prefab or instance) against the face contract. Each rule comes from
  /// an import incident: a mesh named like a bone turns the Humanoid rig Generic, shape keys
  /// exported non-zero become the rest face, an <c>X_mid</c> with no <c>X</c> is a bad export,
  /// and an unwired face or blink fails silently in game.
  /// </summary>
  public static class FaceRigValidator
  {
    private const float DefaultWeightTolerance = 0.01f;

    /// <param name="expressions">Expressions this character must show; channels they name
    /// that the rig lacks are reported as warnings. Null skips the coverage check.</param>
    public static List<FaceRigIssue> Check(GameObject root, IEnumerable<FaceExpressionSO> expressions = null)
    {
      var issues = new List<FaceRigIssue>();
      if (root == null)
      {
        issues.Add(Error("No character given.", null));
        return issues;
      }

      CheckMeshBoneNames(root, issues);
      CheckAvatar(root, issues);

      FaceController face = root.GetComponentInChildren<FaceController>(true);
      if (face == null)
      {
        issues.Add(Error($"'{root.name}' has no FaceController.", root));
        return issues;
      }

      FaceRig rig = FaceRig.Build(face.CollectRenderers());
      CheckRig(face, rig, issues);
      CheckWiring(root, face, issues);
      if (expressions != null) CheckCoverage(rig, expressions, issues);

      return issues;
    }

    private static void CheckMeshBoneNames(GameObject root, List<FaceRigIssue> issues)
    {
      SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
      var boneNames = new HashSet<string>();
      foreach (SkinnedMeshRenderer renderer in renderers)
      {
        foreach (Transform bone in renderer.bones)
        {
          if (bone != null) boneNames.Add(bone.name);
        }
      }

      foreach (SkinnedMeshRenderer renderer in renderers)
      {
        if (boneNames.Contains(renderer.name))
        {
          issues.Add(Error(
            $"Mesh '{renderer.name}' shares its name with a bone. Unity's Humanoid mapper can silently " +
            "fall back to a Generic rig (T-pose). Rename the mesh in the source file.", renderer));
        }
      }
    }

    private static void CheckAvatar(GameObject root, List<FaceRigIssue> issues)
    {
      Animator animator = root.GetComponentInChildren<Animator>(true);
      if (animator == null)
      {
        issues.Add(Error($"'{root.name}' has no Animator.", root));
        return;
      }

      Avatar avatar = animator.avatar;
      if (avatar == null)
      {
        issues.Add(Error($"Animator on '{animator.name}' has no avatar.", animator));
      }
      else if (!avatar.isValid || !avatar.isHuman)
      {
        issues.Add(Error(
          $"Avatar '{avatar.name}' is not a valid Humanoid avatar. If auto-mapping failed, copy the " +
          "human description from a working BASE avatar.", avatar));
      }
    }

    private static void CheckRig(FaceController face, FaceRig rig, List<FaceRigIssue> issues)
    {
      if (rig.Renderers.Count == 0)
      {
        issues.Add(Error($"'{face.name}' has no meshes with shape keys under its FaceController.", face));
        return;
      }

      foreach (SkinnedMeshRenderer renderer in rig.Renderers)
      {
        Mesh mesh = renderer.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
          float weight = renderer.GetBlendShapeWeight(i);
          if (weight > DefaultWeightTolerance)
          {
            issues.Add(Error(
              $"'{renderer.name}' shape '{mesh.GetBlendShapeName(i)}' rests at {weight:0.#}. Shape keys must be " +
              "exported at 0, or they become the rest face.", renderer));
          }
        }
      }

      foreach (string orphan in rig.OrphanMidKeys)
      {
        issues.Add(Error($"'{orphan}' has no base shape '{orphan.Substring(0, orphan.Length - FaceRig.MidSuffix.Length)}' on any mesh.", face));
      }

      foreach (string channel in face.BlinkChannels)
      {
        if (!rig.HasChannel(channel))
        {
          issues.Add(Error($"Blink channel '{channel}' is on no mesh, so the character never blinks.", face));
        }
      }

      if (face.Rest == null)
      {
        issues.Add(Error($"FaceController on '{face.name}' has no rest expression.", face));
      }
    }

    private static void CheckWiring(GameObject root, FaceController face, List<FaceRigIssue> issues)
    {
      if (face.GetComponent<Blinker>() == null)
      {
        issues.Add(Warning($"'{face.name}' has no Blinker.", face));
      }

      EyeAim eyeAim = root.GetComponentInChildren<EyeAim>(true);
      if (eyeAim == null)
      {
        issues.Add(Warning($"'{root.name}' has no EyeAim; the eyes will not follow the look-at target.", root));
      }
      else if (!eyeAim.Resolve())
      {
        issues.Add(Error(
          $"EyeAim on '{eyeAim.name}' can't find both eyes and a target (humanoid eye bones, or a " +
          "HumonoidCharacter look-at target).", eyeAim));
      }
    }

    private static void CheckCoverage(FaceRig rig, IEnumerable<FaceExpressionSO> expressions, List<FaceRigIssue> issues)
    {
      foreach (FaceExpressionSO expression in expressions)
      {
        if (expression == null) continue;

        var missing = new List<string>();
        foreach (FaceWeight weight in expression.Weights)
        {
          if (weight.Weight > 0f && !rig.HasChannel(weight.Name)) missing.Add(weight.Name);
        }

        if (missing.Count > 0)
        {
          issues.Add(Warning($"Expression '{expression.name}' moves channels this face lacks: {string.Join(", ", missing)}.", expression));
        }
      }
    }

    private static FaceRigIssue Error(string message, Object context) => new(FaceRigIssueSeverity.Error, message, context);
    private static FaceRigIssue Warning(string message, Object context) => new(FaceRigIssueSeverity.Warning, message, context);
  }
}
