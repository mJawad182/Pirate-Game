using UnityEngine;
using WaveHarmonic.Crest;

/// <summary>
/// Disables or minimizes LOD in Crest's core system
/// Sets LOD levels to minimum safe value (2) to minimize LOD system
/// Note: Setting to 1 causes array index errors in Crest
/// </summary>
public class CrestLodDisabler : MonoBehaviour
{
    [Header("LOD Disable Settings")]
    [Tooltip("Disable/minimize LOD system (set to minimum safe levels)")]
    public bool disableLod = true;

    [Tooltip("Minimum LOD levels when disabled (2 = minimum safe value, 1 causes IndexOutOfRangeException)")]
    [Range(2, 15)]
    public int minLodLevels = 2;

    [Tooltip("Automatically find WaterRenderer")]
    public bool autoFindWaterRenderer = true;

    [Tooltip("WaterRenderer component (will be found automatically if not set)")]
    public WaterRenderer waterRenderer;

    [Header("Update Settings")]
    [Tooltip("Continuously enforce LOD disable (in case other scripts try to change it)")]
    public bool enforceContinuously = false;

    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;

    private bool isInitialized = false;
    private int originalLodLevels = 7;

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Initializes the LOD disabler
    /// </summary>
    public void Initialize()
    {
        // Find WaterRenderer if not assigned
        if (waterRenderer == null && autoFindWaterRenderer)
        {
            waterRenderer = FindObjectOfType<WaterRenderer>();
        }

        if (waterRenderer == null)
        {
            if (showDebug) Debug.LogError("CrestLodDisabler: WaterRenderer not found!");
            enabled = false;
            return;
        }

        // Store original LOD levels
        originalLodLevels = waterRenderer.LodLevels;

        if (disableLod)
        {
            ApplyLodDisable();
        }

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || waterRenderer == null) return;

        if (disableLod && enforceContinuously)
        {
            // Continuously enforce minimum LOD levels
            if (waterRenderer.LodLevels > minLodLevels)
            {
                waterRenderer.LodLevels = minLodLevels;
                if (showDebug) Debug.Log($"CrestLodDisabler: Enforced LOD levels to {minLodLevels}");
            }
        }
    }

    /// <summary>
    /// Applies LOD disable settings
    /// </summary>
    public void ApplyLodDisable()
    {
        if (waterRenderer == null) return;

        if (disableLod)
        {
            waterRenderer.LodLevels = minLodLevels;
            if (showDebug) Debug.Log($"CrestLodDisabler: Set LOD levels to minimum ({minLodLevels})");
        }
        else
        {
            waterRenderer.LodLevels = originalLodLevels;
            if (showDebug) Debug.Log($"CrestLodDisabler: Restored LOD levels to {originalLodLevels}");
        }
    }

    /// <summary>
    /// Enables or disables LOD system
    /// </summary>
    public void SetLodDisabled(bool disabled)
    {
        disableLod = disabled;
        if (isInitialized)
        {
            ApplyLodDisable();
        }
    }

    void OnDisable()
    {
        // Optionally restore original LOD levels when disabled
        // Uncomment if you want to restore when disabled
        // if (waterRenderer != null && !disableLod)
        // {
        //     waterRenderer.LodLevels = originalLodLevels;
        // }
    }
}
