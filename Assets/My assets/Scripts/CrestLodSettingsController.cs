using UnityEngine;
using WaveHarmonic.Crest;

/// <summary>
/// Controller for adjusting Crest LOD (Level of Detail) settings
/// Allows changing max render distance, LOD levels, and scale range
/// </summary>
public class CrestLodSettingsController : MonoBehaviour
{
    [Header("LOD Level Settings")]
    [Tooltip("Number of LOD levels (more = renders further, but uses more memory)")]
    [Range(1, 15)]
    public int lodLevels = 7;

    [Tooltip("Automatically find WaterRenderer in scene")]
    public bool autoFindWaterRenderer = true;

    [Tooltip("WaterRenderer component (will be found automatically if not set)")]
    public WaterRenderer waterRenderer;

    [Header("Scale Range Settings")]
    [Tooltip("Minimum water scale (smaller = more detail up close, but less distance)")]
    [Range(0.1f, 100f)]
    public float minScale = 1f;

    [Tooltip("Maximum water scale (larger = renders further, but less detail up close). Set to 0 for no maximum.")]
    [Range(0f, 1000f)]
    public float maxScale = 1000f;

    [Tooltip("Apply scale range settings")]
    public bool applyScaleRange = true;

    [Header("Update Settings")]
    [Tooltip("Update LOD settings every frame (useful if you're changing values at runtime)")]
    public bool updateEveryFrame = false;

    [Tooltip("Enable LOD controller (set to false to disable all LOD modifications)")]
    public bool enableLodController = false;

    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;

    private bool isInitialized = false;
    private int lastLodLevels = -1;
    private Vector2 lastScaleRange = Vector2.zero;

    void Start()
    {
        // Check if CrestLodDisabler is active - if so, don't interfere
        CrestLodDisabler lodDisabler = FindObjectOfType<CrestLodDisabler>();
        if (lodDisabler != null && lodDisabler.disableLod)
        {
            enabled = false; // Disable this script if LOD is being disabled
            return;
        }

        if (enableLodController)
        {
            Initialize();
        }
        else
        {
            enabled = false; // Disable the script if LOD controller is off
        }
    }

    /// <summary>
    /// Initializes the LOD settings controller
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
            if (showDebug) Debug.LogError("CrestLodSettingsController: WaterRenderer not found! Please assign it in the Inspector or ensure one exists in the scene.");
            enabled = false;
            return;
        }

        // Apply initial settings
        ApplyLodSettings();

        isInitialized = true;

        if (showDebug)
        {
            Debug.Log($"CrestLodSettingsController: Initialized with LOD Levels: {lodLevels}, Scale Range: {minScale} - {maxScale}");
        }
    }

    void Update()
    {
        if (!enableLodController || !isInitialized || waterRenderer == null) return;

        if (updateEveryFrame)
        {
            // Check if settings have changed
            bool needsUpdate = false;

            if (waterRenderer.LodLevels != lodLevels)
            {
                needsUpdate = true;
            }

            if (applyScaleRange)
            {
                Vector2 currentScaleRange = waterRenderer.ScaleRange;
                Vector2 targetScaleRange = new Vector2(minScale, maxScale == 0f ? Mathf.Infinity : maxScale);
                
                if (currentScaleRange != targetScaleRange)
                {
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                ApplyLodSettings();
            }
        }
    }

    /// <summary>
    /// Applies LOD settings to the WaterRenderer
    /// </summary>
    public void ApplyLodSettings()
    {
        if (!enableLodController) return; // Don't apply if disabled

        if (waterRenderer == null)
        {
            if (showDebug) Debug.LogError("CrestLodSettingsController: WaterRenderer is null! Cannot apply settings.");
            return;
        }

        // Apply LOD levels
        if (waterRenderer.LodLevels != lodLevels)
        {
            waterRenderer.LodLevels = lodLevels;
            if (showDebug) Debug.Log($"CrestLodSettingsController: Set LOD Levels to {lodLevels}");
        }

        // Apply scale range
        if (applyScaleRange)
        {
            Vector2 scaleRange = new Vector2(minScale, maxScale == 0f ? Mathf.Infinity : maxScale);
            
            if (waterRenderer.ScaleRange != scaleRange)
            {
                waterRenderer.ScaleRange = scaleRange;
                if (showDebug) Debug.Log($"CrestLodSettingsController: Set Scale Range to {scaleRange.x} - {scaleRange.y}");
            }
        }
    }

    /// <summary>
    /// Sets the LOD levels
    /// </summary>
    public void SetLodLevels(int levels)
    {
        lodLevels = Mathf.Clamp(levels, 1, 15);
        if (isInitialized)
        {
            ApplyLodSettings();
        }
    }

    /// <summary>
    /// Sets the scale range
    /// </summary>
    public void SetScaleRange(float min, float max)
    {
        minScale = min;
        maxScale = max;
        if (isInitialized)
        {
            ApplyLodSettings();
        }
    }

    /// <summary>
    /// Gets current LOD information for debugging
    /// </summary>
    public void LogCurrentLodInfo()
    {
        if (!enableLodController || waterRenderer == null) return;

        if (showDebug)
        {
            Debug.Log($"CrestLodSettingsController - Current LOD Info:\n" +
                      $"  LOD Levels: {waterRenderer.LodLevels}\n" +
                      $"  Scale Range: {waterRenderer.ScaleRange}\n" +
                      $"  Current Scale: {waterRenderer.Scale}");
        }
    }

    void OnValidate()
    {
        // Clamp values in editor
        lodLevels = Mathf.Clamp(lodLevels, 1, 15);
        minScale = Mathf.Max(0.1f, minScale);
        maxScale = Mathf.Max(0f, maxScale);

        // Apply settings if already initialized and in play mode
        if (Application.isPlaying && isInitialized && waterRenderer != null)
        {
            ApplyLodSettings();
        }
    }
}
