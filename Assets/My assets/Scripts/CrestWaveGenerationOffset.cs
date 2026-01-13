using UnityEngine;
using System.Reflection;
using WaveHarmonic.Crest;

/// <summary>
/// Generates waves at a Transform position using SphereWaterInteraction
/// Keeps camera for LOD/interactions, but creates waves at a custom Transform position
/// </summary>
public class CrestWaveGenerationOffset : MonoBehaviour
{
    [Header("Wave Generation Position")]
    [Tooltip("Transform where waves should be generated (waves will be created here)")]
    public Transform waveGenerationTransform;

    [Tooltip("Automatically find WaterRenderer")]
    public bool autoFindWaterRenderer = true;

    [Tooltip("WaterRenderer component (will be found automatically if not set)")]
    public WaterRenderer waterRenderer;

    [Header("Wave Generation Settings")]
    [Tooltip("Radius of wave generation sphere")]
    [Range(1f, 100f)]
    public float waveRadius = 50f;

    [Tooltip("Intensity of generated waves")]
    [Range(0.1f, 20f)]
    public float waveIntensity = 5f;

    [Tooltip("Speed of wave generation movement (creates continuous waves)")]
    [Range(0.1f, 10f)]
    public float waveMovementSpeed = 2f;

    [Tooltip("Update wave generation every frame")]
    public bool updateEveryFrame = true;

    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;

    private Camera mainCamera;
    private GameObject waveGeneratorObject;
    private SphereWaterInteraction sphereInteraction;
    private Rigidbody waveRigidbody;
    private Vector3 lastPosition;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Initializes the wave generation system
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
            Debug.LogError("CrestWaveGenerationOffset: No camera found! Camera is needed for LOD and interactions.");
            enabled = false;
            return;
        }

        // Find WaterRenderer if not assigned
        if (waterRenderer == null && autoFindWaterRenderer)
        {
            waterRenderer = FindObjectOfType<WaterRenderer>();
        }

        if (waterRenderer == null)
        {
            Debug.LogError("CrestWaveGenerationOffset: WaterRenderer not found! Please assign it in the Inspector or ensure one exists in the scene.");
            enabled = false;
            return;
        }

        // CRITICAL: Always keep Viewpoint as camera (for LOD and interactions)
        if (waterRenderer.Viewpoint != mainCamera.transform)
        {
            waterRenderer.Viewpoint = mainCamera.transform;
            if (showDebug) Debug.Log("CrestWaveGenerationOffset: Set Viewpoint to camera (for LOD and interactions)");
        }

        if (waveGenerationTransform == null)
        {
            if (showDebug) Debug.LogWarning("CrestWaveGenerationOffset: No wave generation transform assigned. Cannot create waves.");
            enabled = false;
            return;
        }

        // Create wave generator GameObject at Transform position
        CreateWaveGenerator();

        isInitialized = true;
    }

    /// <summary>
    /// Creates a GameObject with SphereWaterInteraction at the Transform position
    /// </summary>
    private void CreateWaveGenerator()
    {
        if (waveGenerationTransform == null) return;

        // Create GameObject for wave generation
        waveGeneratorObject = new GameObject("WaveGenerator_" + waveGenerationTransform.name);
        waveGeneratorObject.transform.position = waveGenerationTransform.position;
        waveGeneratorObject.transform.parent = waveGenerationTransform; // Parent to Transform so it follows

        // Add Rigidbody for velocity tracking (required for SphereWaterInteraction)
        waveRigidbody = waveGeneratorObject.AddComponent<Rigidbody>();
        waveRigidbody.isKinematic = true;
        waveRigidbody.useGravity = false;

        // Add SphereWaterInteraction component
        sphereInteraction = waveGeneratorObject.AddComponent<SphereWaterInteraction>();
        
        // Set properties via reflection (since they're internal)
        var radiusField = typeof(SphereWaterInteraction).GetField("_Radius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var weightField = typeof(SphereWaterInteraction).GetField("_Weight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (radiusField != null)
        {
            radiusField.SetValue(sphereInteraction, waveRadius);
        }
        
        if (weightField != null)
        {
            weightField.SetValue(sphereInteraction, waveIntensity);
        }

        lastPosition = waveGenerationTransform.position;

        if (showDebug) Debug.Log($"CrestWaveGenerationOffset: Created wave generator at {waveGenerationTransform.name} with radius {waveRadius} and intensity {waveIntensity}");
    }

    void Update()
    {
        if (!isInitialized || waterRenderer == null || mainCamera == null) return;

        // CRITICAL: Always keep Viewpoint as camera (for LOD and interactions)
        if (waterRenderer.Viewpoint != mainCamera.transform)
        {
            waterRenderer.Viewpoint = mainCamera.transform;
        }

        // Update wave generator position and create movement for wave generation
        if (waveGeneratorObject != null && waveGenerationTransform != null && updateEveryFrame)
        {
            // Update position to follow Transform
            Vector3 currentPosition = waveGenerationTransform.position;
            waveGeneratorObject.transform.position = currentPosition;

            // Calculate velocity for SphereWaterInteraction (it needs movement to generate waves)
            Vector3 velocity = (currentPosition - lastPosition) / Time.deltaTime;
            
            // If Transform is not moving, create small circular movement to generate continuous waves
            if (velocity.magnitude < 0.1f)
            {
                float angle = Time.time * waveMovementSpeed;
                float x = Mathf.Cos(angle) * waveRadius * 0.1f;
                float z = Mathf.Sin(angle) * waveRadius * 0.1f;
                Vector3 offset = new Vector3(x, 0, z);
                waveGeneratorObject.transform.position = currentPosition + offset;
                velocity = (waveGeneratorObject.transform.position - lastPosition) / Time.deltaTime;
            }

            // Set Rigidbody velocity (SphereWaterInteraction uses this)
            if (waveRigidbody != null)
            {
                waveRigidbody.linearVelocity = velocity;
            }

            lastPosition = waveGeneratorObject.transform.position;
        }
    }

    /// <summary>
    /// Sets the wave generation transform
    /// </summary>
    public void SetWaveGenerationTransform(Transform newTransform)
    {
        waveGenerationTransform = newTransform;
        
        // Recreate wave generator if transform changes
        if (waveGeneratorObject != null)
        {
            Destroy(waveGeneratorObject);
            waveGeneratorObject = null;
        }
        
        if (newTransform != null && isInitialized)
        {
            CreateWaveGenerator();
            if (showDebug)
            {
                Debug.Log($"CrestWaveGenerationOffset: Wave generation transform set to {newTransform.name}");
            }
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
        // Clean up wave generator
        if (waveGeneratorObject != null)
        {
            Destroy(waveGeneratorObject);
        }

        // Ensure Viewpoint is restored to camera when destroyed
        if (waterRenderer != null && mainCamera != null)
        {
            waterRenderer.Viewpoint = mainCamera.transform;
        }
    }
}
