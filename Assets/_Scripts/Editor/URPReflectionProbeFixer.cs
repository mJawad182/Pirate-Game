using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Fixes URP Reflection Probe array size issues.
/// Access via: Tools > Pirate Escape Room > Fix Reflection Probes
/// </summary>
public class URPReflectionProbeFixer : EditorWindow
{
    [MenuItem("Tools/Pirate Escape Room/Fix Reflection Probes")]
    public static void ShowWindow()
    {
        GetWindow<URPReflectionProbeFixer>("Fix Reflection Probes");
    }
    
    [MenuItem("Tools/Pirate Escape Room/Fix Reflection Probes (Quick)")]
    public static void QuickFix()
    {
        FixReflectionProbes();
        EditorUtility.DisplayDialog("Fix Complete", 
            "Reflection probe issues have been addressed.\n\n" +
            "If the error persists, restart Unity as suggested.", "OK");
    }
    
    void OnGUI()
    {
        GUILayout.Label("URP Reflection Probe Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This error occurs when there are too many reflection probes in the scene " +
            "or URP settings exceed the maximum array size.\n\n" +
            "Solutions:\n" +
            "1. Remove unnecessary reflection probes\n" +
            "2. Adjust URP asset reflection probe settings\n" +
            "3. Restart Unity to recreate arrays", 
            MessageType.Info);
        
        GUILayout.Space(10);
        
        // Count reflection probes
        ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>();
        GUILayout.Label($"Found {probes.Length} Reflection Probe(s) in scene", EditorStyles.label);
        
        if (probes.Length > 32)
        {
            EditorGUILayout.HelpBox(
                $"Warning: {probes.Length} probes found. URP typically supports up to 32-64 probes. " +
                "Consider removing unnecessary probes.", 
                MessageType.Warning);
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("List All Reflection Probes", GUILayout.Height(30)))
        {
            ListReflectionProbes();
        }
        
        if (GUILayout.Button("Remove Disabled Reflection Probes", GUILayout.Height(30)))
        {
            RemoveDisabledProbes();
        }
        
        if (GUILayout.Button("Fix URP Asset Settings", GUILayout.Height(30)))
        {
            FixURPAssetSettings();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Run All Fixes", GUILayout.Height(40)))
        {
            FixReflectionProbes();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Note: You may need to restart Unity after making changes " +
            "to fully resolve the array size issue.", 
            MessageType.Warning);
    }
    
    static void FixReflectionProbes()
    {
        Undo.SetCurrentGroupName("Fix Reflection Probes");
        int undoGroup = Undo.GetCurrentGroup();
        
        // Remove disabled probes
        int removed = RemoveDisabledProbes();
        
        // Fix URP settings
        FixURPAssetSettings();
        
        // List remaining probes
        ReflectionProbe[] remaining = FindObjectsOfType<ReflectionProbe>();
        
        Debug.Log($"=== Reflection Probe Fix Complete ===");
        Debug.Log($"Removed {removed} disabled probe(s)");
        Debug.Log($"Remaining probes: {remaining.Length}");
        
        if (remaining.Length > 32)
        {
            Debug.LogWarning($"Still have {remaining.Length} probes. Consider removing more if error persists.");
        }
        
        Undo.CollapseUndoOperations(undoGroup);
    }
    
    static int RemoveDisabledProbes()
    {
        ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>();
        int removed = 0;
        
        foreach (ReflectionProbe probe in probes)
        {
            if (probe != null && !probe.enabled)
            {
                Undo.DestroyObjectImmediate(probe.gameObject);
                removed++;
            }
        }
        
        if (removed > 0)
        {
            Debug.Log($"Removed {removed} disabled reflection probe(s)");
        }
        
        return removed;
    }
    
    static void ListReflectionProbes()
    {
        ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>();
        
        Debug.Log($"=== Reflection Probes in Scene ({probes.Length}) ===");
        for (int i = 0; i < probes.Length; i++)
        {
            ReflectionProbe probe = probes[i];
            string status = probe.enabled ? "Enabled" : "Disabled";
            Debug.Log($"{i + 1}. {probe.name} - {status} - Mode: {probe.mode}");
        }
    }
    
    static void FixURPAssetSettings()
    {
        // Try to find URP Asset
        UniversalRenderPipelineAsset urpAsset = null;
        
        // Check Graphics Settings
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
        {
            urpAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        }
        
        // Search project for URP assets
        if (urpAsset == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            }
        }
        
        if (urpAsset != null)
        {
            Debug.Log($"Found URP Asset: {urpAsset.name}");
            Debug.Log("Note: Reflection probe limits are typically hardcoded in URP.");
            Debug.Log("Consider reducing the number of reflection probes in your scene.");
        }
        else
        {
            Debug.LogWarning("Could not find URP Asset. Make sure URP is set as your render pipeline.");
        }
    }
}



