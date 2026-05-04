#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CupkekGames.Services;

namespace CupkekGames.Character
{
  [CustomEditor(typeof(HumonoidCharacter))]
  public class HumonoidCharacterEditor : UnityEditor.Editor
  {
    private string _selectedExpression = BlendShapeKinds.Neutral;
    private string _selectedAnimation = AnimationClipKinds.None;
    private float _expressionDuration = 2f;

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      HumonoidCharacter character = (HumonoidCharacter)target;

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox("These controls only work in Play Mode", MessageType.Info);

      // Expression Section
      EditorGUILayout.Space(5);
      EditorGUILayout.LabelField("Expression", EditorStyles.boldLabel);

      _selectedExpression = EditorGUILayout.TextField("Expression", _selectedExpression);
      _expressionDuration = EditorGUILayout.FloatField("Duration (seconds)", _expressionDuration);

      EditorGUI.BeginDisabledGroup(!Application.isPlaying);
      if (GUILayout.Button("Play Expression"))
      {
        if (Application.isPlaying)
        {
          BlendShapeDatabase blendShapeDatabase = ServiceLocator.Get<BlendShapeDatabase>(true);
          if (blendShapeDatabase != null)
          {
            character.PlayExpression(blendShapeDatabase, _selectedExpression, _expressionDuration).Forget();
            Debug.Log($"Playing expression: {_selectedExpression} for {_expressionDuration} seconds");
          }
          else
          {
            Debug.LogError("BlendShapeDatabase not found in ServiceLocator");
          }
        }
      }

      EditorGUI.EndDisabledGroup();

      // Animation Section
      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);

      _selectedAnimation = EditorGUILayout.TextField("Animation Clip", _selectedAnimation);

      EditorGUI.BeginDisabledGroup(!Application.isPlaying || string.IsNullOrEmpty(_selectedAnimation));
      if (GUILayout.Button("Play Animation"))
      {
        if (Application.isPlaying)
        {
          AnimationDatabase animationDatabase = ServiceLocator.Get<AnimationDatabase>(true);
          if (animationDatabase != null)
          {
            var clipData = animationDatabase.GetAnimation(_selectedAnimation);
            if (clipData != null)
            {
              character.PlayAnimation(clipData.Clip, clipData.FadeDuration);
            }
            Debug.Log($"Playing animation: {_selectedAnimation}");
          }
          else
          {
            Debug.LogError("AnimationDatabase not found in ServiceLocator");
          }
        }
      }

      EditorGUI.EndDisabledGroup();
    }
  }
}
#endif
