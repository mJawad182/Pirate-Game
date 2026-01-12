using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for GameManager that adds a "Quick Setup" button
/// </summary>
[CustomEditor(typeof(GameManager))]
public class PirateEscapeRoomSetupInspector : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Setup button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("🔧 One-Click Setup", GUILayout.Height(30), GUILayout.Width(200)))
        {
            PirateEscapeRoomSetup.QuickSetup();
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox("Click to automatically create and configure all systems.", MessageType.Info);
    }
}



