#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CupkekGames.Character
{
  [CustomEditor(typeof(CharacterVisualAccessories))]
  public class CharacterVisualAccessoriesEditor : UnityEditor.Editor
  {
    private string _selectedRole = VisualAccessoryRoleKinds.DEFAULT;

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      CharacterVisualAccessories accessories = (CharacterVisualAccessories)target;

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Equipment Controls", EditorStyles.boldLabel);

      EditorGUILayout.Space(5);

      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button("Enable Equipments (Default)"))
      {
        accessories.EnableEquipments();
        EditorUtility.SetDirty(accessories);
        Debug.Log("Enabled equipments (DEFAULT role)");
      }

      if (GUILayout.Button("Disable Equipments"))
      {
        accessories.DisableEquipments();
        EditorUtility.SetDirty(accessories);
        Debug.Log("Disabled all equipments");
      }

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space(5);

      EditorGUILayout.LabelField("Enable Equipment by Role", EditorStyles.boldLabel);
      _selectedRole = EditorGUILayout.TextField("Role", _selectedRole);

      if (GUILayout.Button($"Enable Equipment ({_selectedRole})"))
      {
        accessories.EnableEquipment(_selectedRole);
        EditorUtility.SetDirty(accessories);
        Debug.Log($"Enabled equipments with role: {_selectedRole}");
      }
    }
  }
}
#endif
