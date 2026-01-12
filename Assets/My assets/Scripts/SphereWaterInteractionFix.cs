using UnityEngine;
using WaveHarmonic.Crest;
using System.Reflection;

/// <summary>
/// Fixes SphereWaterInteraction pushing player above water
/// Moves interaction point down and reduces weight to prevent unwanted buoyancy
/// </summary>
[RequireComponent(typeof(SphereWaterInteraction))]
public class SphereWaterInteractionFix : MonoBehaviour
{
    [Header("Fix Settings")]
    [Tooltip("Reduce weight to prevent pushing player up (0.1-1.0)")]
    [Range(0.1f, 1f)]
    public float weightMultiplier = 0.3f;
    
    [Tooltip("Reduce radius to minimize area of effect")]
    [Range(0.3f, 1f)]
    public float radiusMultiplier = 0.6f;
    
    [Tooltip("Move interaction point down below player (negative = below)")]
    public float verticalOffset = -2f;
    
    [Tooltip("Apply fixes automatically")]
    public bool applyOnStart = true;
    
    private SphereWaterInteraction sphereInteraction;
    private float originalWeight;
    private float originalRadius;
    private Vector3 originalLocalPosition;
    
    void Start()
    {
        sphereInteraction = GetComponent<SphereWaterInteraction>();
        
        if (sphereInteraction == null)
        {
            Debug.LogError("SphereWaterInteractionFix: No SphereWaterInteraction found!");
            enabled = false;
            return;
        }
        
        // Store original values
        StoreOriginalValues();
        
        if (applyOnStart)
        {
            ApplyFixes();
        }
    }
    
    void StoreOriginalValues()
    {
        var weightField = typeof(SphereWaterInteraction).GetField("_Weight", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var radiusField = typeof(SphereWaterInteraction).GetField("_Radius", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (weightField != null)
        {
            originalWeight = (float)weightField.GetValue(sphereInteraction);
        }
        else
        {
            originalWeight = 1f; // Default
        }
        
        if (radiusField != null)
        {
            originalRadius = (float)radiusField.GetValue(sphereInteraction);
        }
        else
        {
            originalRadius = 1f; // Default
        }
        
        originalLocalPosition = transform.localPosition;
    }
    
    void ApplyFixes()
    {
        if (sphereInteraction == null) return;
        
        // Reduce weight to prevent pushing player up
        var weightField = typeof(SphereWaterInteraction).GetField("_Weight", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (weightField != null)
        {
            float newWeight = originalWeight * weightMultiplier;
            weightField.SetValue(sphereInteraction, newWeight);
        }
        
        // Reduce radius to minimize effect area
        var radiusField = typeof(SphereWaterInteraction).GetField("_Radius", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (radiusField != null)
        {
            float newRadius = originalRadius * radiusMultiplier;
            radiusField.SetValue(sphereInteraction, newRadius);
        }
        
        // Move interaction point down (below player center)
        if (verticalOffset != 0f)
        {
            Vector3 newPosition = originalLocalPosition;
            newPosition.y += verticalOffset;
            transform.localPosition = newPosition;
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && sphereInteraction != null)
        {
            ApplyFixes();
        }
    }
    
    /// <summary>
    /// Reset to original values
    /// </summary>
    public void ResetToOriginal()
    {
        var weightField = typeof(SphereWaterInteraction).GetField("_Weight", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var radiusField = typeof(SphereWaterInteraction).GetField("_Radius", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (weightField != null)
        {
            weightField.SetValue(sphereInteraction, originalWeight);
        }
        
        if (radiusField != null)
        {
            radiusField.SetValue(sphereInteraction, originalRadius);
        }
        
        transform.localPosition = originalLocalPosition;
    }
}
