using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CupkekGames.Character.Editor
{
  [CustomEditor(typeof(FaceController))]
  public class FaceControllerEditor : UnityEditor.Editor
  {
    private FaceExpressionSO _preview;
    private bool _showChannels;
    private List<FaceRigIssue> _issues;

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      var face = (FaceController)target;

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
      _preview = (FaceExpressionSO)EditorGUILayout.ObjectField("Expression", _preview, typeof(FaceExpressionSO), false);

      using (new EditorGUILayout.HorizontalScope())
      {
        using (new EditorGUI.DisabledScope(_preview == null))
        {
          if (GUILayout.Button(Application.isPlaying ? "Play" : "Snap"))
          {
            if (Application.isPlaying) face.Play(_preview);
            else Snap(face, _preview);
          }
        }

        using (new EditorGUI.DisabledScope(face.Rest == null))
        {
          if (GUILayout.Button("Rest"))
          {
            if (Application.isPlaying) face.Clear();
            else Snap(face, face.Rest);
          }
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying || face.GetComponent<Blinker>() == null))
        {
          if (GUILayout.Button("Blink")) face.GetComponent<Blinker>().BlinkNow();
        }
      }

      if (!Application.isPlaying)
      {
        EditorGUILayout.HelpBox("Snap writes the weights into the meshes; press Rest before saving.", MessageType.None);
      }

      EditorGUILayout.Space(6);
      _showChannels = EditorGUILayout.Foldout(_showChannels, "Rig", true);
      if (_showChannels)
      {
        if (GUILayout.Button("Rebuild")) face.RebuildRig();

        FaceRig rig = face.Rig;
        EditorGUILayout.LabelField($"{rig.Renderers.Count} meshes, {rig.ChannelNames.Count()} channels");
        using (new EditorGUI.IndentLevelScope())
        {
          foreach (SkinnedMeshRenderer renderer in rig.Renderers)
          {
            EditorGUILayout.ObjectField(renderer, typeof(SkinnedMeshRenderer), true);
          }

          foreach (string channel in rig.ChannelNames.OrderBy(name => name))
          {
            EditorGUILayout.LabelField(rig.HasMid(channel) ? $"{channel}  (+ mid)" : channel);
          }
        }
      }

      EditorGUILayout.Space(6);
      if (GUILayout.Button("Validate"))
      {
        HumonoidCharacter character = face.GetComponentInParent<HumonoidCharacter>();
        _issues = FaceRigValidator.Check(character != null ? character.gameObject : face.gameObject);
      }

      if (_issues != null)
      {
        if (_issues.Count == 0) EditorGUILayout.HelpBox("No issues.", MessageType.Info);
        foreach (FaceRigIssue issue in _issues)
        {
          EditorGUILayout.HelpBox(issue.Message,
            issue.Severity == FaceRigIssueSeverity.Error ? MessageType.Error : MessageType.Warning);
        }
      }
    }

    private static void Snap(FaceController face, FaceExpressionSO expression)
    {
      foreach (SkinnedMeshRenderer renderer in face.Rig.Renderers)
      {
        Undo.RecordObject(renderer, "Face preview");
      }

      face.Snap(expression);
    }
  }
}
