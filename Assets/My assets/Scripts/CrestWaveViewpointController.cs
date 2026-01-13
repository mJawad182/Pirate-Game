using UnityEngine;
using System.Reflection;
using WaveHarmonic.Crest;

/// <summary>
/// Offsets wave generation position without affecting LOD or water interactions
/// Keeps camera for LOD/interactions, but uses custom GameObject for wave generation offset
/// </summary>
public class CrestWaveViewpointController : MonoBehaviour
{
    [Header("Wave Generation Offset Settings")]
    [Tooltip("Custom GameObject to use for wave generation offset. Waves will be generated relative to this position.")]
    public Transform customWaveViewpoint;

    [Tooltip("Automatically find WaterRenderer and ShapeGerstner components")]
    public bool autoFindComponents = true;

    [Tooltip("WaterRenderer component (will be found automatically if not set)")]
    public WaterRenderer waterRenderer;

    [Tooltip("ShapeGerstner components (will be found automatically if not set)")]
    public ShapeGerstner[] shapeGerstnerComponents;

    [Header("Update Settings")]
    [Tooltip("Update wave generation offset every frame")]
    public bool updateEveryFrame = true;

    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;

    private Camera mainCamera;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Initializes the wave generation offset controller
    /// </summary>
    public void Initialize()
    {
        // Find main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("CrestWaveViewpointController: No camera found! Camera is needed for LOD and interactions.");
            enabled = false;
            return;
        }

        // Find WaterRenderer if not assigned
        if (waterRenderer == null && autoFindComponents)
        {
            waterRenderer = FindObjectOfType<WaterRenderer>();
        }

        if (waterRenderer == null)
        {
            Debug.LogError("CrestWaveViewpointController: WaterRenderer not found! Please assign it in the Inspector or ensure one exists in the scene.");
            enabled = false;
            return;
        }

        // Set Viewpoint to custom GameObject if provided, otherwise use camera
        if (customWaveViewpoint != null)
        {
            waterRenderer.Viewpoint = customWaveViewpoint;
            if (showDebug) Debug.Log($"CrestWaveViewpointController: Set Viewpoint to {customWaveViewpoint.name} for wave generation");
        }
        else
        {
            waterRenderer.Viewpoint = mainCamera.transform;
            if (showDebug) Debug.Log("CrestWaveViewpointController: No custom viewpoint set, using camera");
        }

        // Find ShapeGerstner components if not assigned
        if (shapeGerstnerComponents == null || shapeGerstnerComponents.Length == 0)
        {
            if (autoFindComponents)
            {
                shapeGerstnerComponents = FindObjectsOfType<ShapeGerstner>();
            }
        }

        if (shapeGerstnerComponents == null || shapeGerstnerComponents.Length == 0)
        {
            Debug.LogWarning("CrestWaveViewpointController: No ShapeGerstner components found! Wave generation offset may not work properly.");
        }
        else
        {
            if (showDebug) Debug.Log($"CrestWaveViewpointController: Found {shapeGerstnerComponents.Length} ShapeGerstner component(s)");
        }

        if (customWaveViewpoint == null)
        {
            if (showDebug) Debug.LogWarning("CrestWaveViewpointController: No custom wave viewpoint assigned. Wave generation will use camera position.");
        }
        else
        {
            if (showDebug) Debug.Log($"CrestWaveViewpointController: Wave generation will be offset relative to {customWaveViewpoint.name}");
        }

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || waterRenderer == null) return;

        // Update Viewpoint to custom GameObject if set
        if (customWaveViewpoint != null)
        {
            // Set Viewpoint to custom GameObject for wave generation
            if (waterRenderer.Viewpoint != customWaveViewpoint)
            {
                waterRenderer.Viewpoint = customWaveViewpoint;
                if (showDebug) Debug.Log($"CrestWaveViewpointController: Viewpoint set to {customWaveViewpoint.name}");
            }

            // Update position if GameObject moves
            if (updateEveryFrame && waterRenderer.Viewpoint != null)
            {
                // The Viewpoint Transform will automatically follow the customWaveViewpoint
                // since we set it to the same Transform reference
            }
        }
        else
        {
            // Fallback to camera if no custom viewpoint
            if (mainCamera != null && waterRenderer.Viewpoint != mainCamera.transform)
            {
                waterRenderer.Viewpoint = mainCamera.transform;
            }
        }
    }


    /// <summary>
    /// Sets the custom wave generation viewpoint GameObject
    /// </summary>
    public void SetCustomWaveViewpoint(Transform newViewpoint)
    {
        customWaveViewpoint = newViewpoint;
        if (showDebug && newViewpoint != null)
        {
            Debug.Log($"CrestWaveViewpointController: Custom wave generation viewpoint set to {newViewpoint.name}");
        }
    }

    void OnDisable()
    {
        // Ensure Viewpoint is restored to camera when disabled
        if (waterRenderer != null && mainCamera != null)
        {
            waterRenderer.Viewpoint = mainCamera.transform;
        }
    }

    void OnDestroy()
    {
        // Ensure Viewpoint is restored to camera when destroyed
        if (waterRenderer != null && mainCamera != null)
        {
            waterRenderer.Viewpoint = mainCamera.transform;
        }
    }
}
