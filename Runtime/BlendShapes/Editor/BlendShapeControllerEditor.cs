#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CupkekGames.Character.Editor
{
  [CustomEditor(typeof(BlendShapeController))]
  public class BlendShapeControllerEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      // Draw default inspector
      DrawDefaultInspector();

      BlendShapeController controller = (BlendShapeController)target;

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

      // Button 1: Copy from SO to TargetBlendShapes
      EditorGUI.BeginDisabledGroup(controller.TargetSO == null || controller.TargetSO.BlendShapes == null);
      if (GUILayout.Button("Copy from SO to TargetBlendShapes"))
      {
        if (controller.TargetSO != null && controller.TargetSO.BlendShapes != null)
        {
          // Store reference to SO and ensure it's not marked dirty
          var sourceSO = controller.TargetSO;

          // Explicitly clear dirty flag on SO before we start
          EditorUtility.ClearDirty(sourceSO);

          // Create a new list with copies of the BlendShapeData structs
          // This ensures we don't modify the ScriptableObject's list
          var newList = new List<BlendShapeData>();
          for (int i = 0; i < sourceSO.BlendShapes.Count; i++)
          {
            // Create a copy of the BlendShapeData struct (value type, so this creates a copy)
            newList.Add(new BlendShapeData
            {
              Name = sourceSO.BlendShapes[i].Name,
              Weight = sourceSO.BlendShapes[i].Weight
            });
          }

          // CRITICAL: Break any existing reference BEFORE modifying through SerializedProperty
          // This ensures Unity doesn't link TargetBlendShapes to the SO's list
          Undo.RecordObject(controller, "Copy BlendShapes from SO");
          controller.TargetBlendShapes = new List<BlendShapeData>();

          serializedObject.Update();

          // Use SerializedProperty to modify the list for proper undo/redo support
          SerializedProperty targetBlendShapesProp = serializedObject.FindProperty("TargetBlendShapes");
          targetBlendShapesProp.ClearArray();

          for (int i = 0; i < newList.Count; i++)
          {
            targetBlendShapesProp.InsertArrayElementAtIndex(i);
            SerializedProperty elementProp = targetBlendShapesProp.GetArrayElementAtIndex(i);

            elementProp.FindPropertyRelative("Name").stringValue = newList[i].Name;
            elementProp.FindPropertyRelative("Weight").floatValue = newList[i].Weight;
          }

          // Apply changes to the GameObject only
          serializedObject.ApplyModifiedProperties();

          // Explicitly break any reference by reassigning the list after serialization
          // This ensures TargetBlendShapes is a completely independent list
          controller.TargetBlendShapes = new List<BlendShapeData>(newList);

          // Only mark the GameObject as dirty, never the ScriptableObject
          EditorUtility.SetDirty(controller);

          // Explicitly ensure the ScriptableObject is NOT marked as dirty
          // This prevents Unity from saving changes to the SO
          EditorUtility.ClearDirty(sourceSO);

          Debug.Log(
            $"Copied {sourceSO.BlendShapes.Count} blend shapes from SO to TargetBlendShapes (created new list instance)");
        }
      }

      EditorGUI.EndDisabledGroup();

      // Button 2: Apply TargetBlendShapes to Mesh
      EditorGUI.BeginDisabledGroup(controller.TargetBlendShapes == null || controller.TargetBlendShapes.Count == 0);
      if (GUILayout.Button("Apply TargetBlendShapes to Mesh"))
      {
        controller.ApplyValuesFromTarget();
        Debug.Log($"Applied {controller.TargetBlendShapes.Count} blend shapes to mesh");
      }

      EditorGUI.EndDisabledGroup();
    }
  }
}
#endif
