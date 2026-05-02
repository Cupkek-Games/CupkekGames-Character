#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CupkekGames.Character
{
  [CustomEditor(typeof(CharacterVisualAccessories))]
  public class CharacterVisualAccessoriesEditor : UnityEditor.Editor
  {
    private VisualAccessoryRole _selectedRole = VisualAccessoryRole.DEFAULT;

    public override void OnInspectorGUI()
    {
      // Draw default inspector
      DrawDefaultInspector();

      CharacterVisualAccessories accessories = (CharacterVisualAccessories)target;

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Equipment Controls", EditorStyles.boldLabel);

      EditorGUILayout.Space(5);

      // Enable/Disable buttons
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

      // Role selection and enable with specific role
      EditorGUILayout.LabelField("Enable Equipment by Role", EditorStyles.boldLabel);
      _selectedRole = (VisualAccessoryRole)EditorGUILayout.EnumPopup("Role", _selectedRole);

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
