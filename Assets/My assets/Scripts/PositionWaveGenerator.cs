using UnityEngine;
using WaveHarmonic.Crest;

/// <summary>
/// Creates waves at a specific GameObject's position.
/// Can be triggered on demand or continuously active.
/// </summary>
public class PositionWaveGenerator : MonoBehaviour
{
    [Header("Position Reference")]
    [Tooltip("GameObject whose position will be used for wave generation. If null, uses this GameObject's position.")]
    public GameObject positionReference;
    
    [Header("Wave Settings")]
    [Tooltip("Radius of the wave effect")]
    [Range(1f, 50f)]
    public float waveRadius = 5f;
    
    [Tooltip("Intensity/strength of the waves")]
    [Range(0.1f, 50f)]
    public float waveIntensity = 10f;
    
    [Header("Activation")]
    [Tooltip("If enabled, waves are generated continuously. If disabled, use GenerateWave() method.")]
    public bool continuousWaves = true;
    
    [Tooltip("Minimum time between wave pulses (for continuous mode)")]
    [Range(0.1f, 5f)]
    public float pulseInterval = 0.5f;
    
    [Header("One-Time Wave")]
    [Tooltip("Duration for one-time wave pulse (seconds)")]
    [Range(0.1f, 10f)]
    public float waveDuration = 2f;
    
    [Header("Debug")]
    [Tooltip("Show debug gizmos in scene view")]
    public bool showDebugGizmos = true;
    
    private GameObject waveObject;
    private SphereWaterInteraction waveInteraction;
    private float lastPulseTime;
    private bool isWaveActive;
    private float waveStartTime;
    private Vector3 previousPosition;
    private Rigidbody waveRigidbody;
    
    void Start()
    {
        CreateWaveObject();
        
        if (continuousWaves)
        {
            ActivateWave();
        }
    }
    
    void Update()
    {
        if (waveObject == null || waveInteraction == null) return;
        
        Vector3 targetPosition = positionReference != null ? 
            positionReference.transform.position : transform.position;
        
        // Create movement to generate waves (SphereWaterInteraction needs velocity)
        // Move slightly up and down or in a small circle to create continuous waves
        if (continuousWaves && isWaveActive)
        {
            // Create small circular motion to generate waves
            float time = Time.time;
            float motionRadius = 0.1f; // Small movement radius
            Vector3 offset = new Vector3(
                Mathf.Sin(time * 2f) * motionRadius,
                Mathf.Cos(time * 2f) * motionRadius * 0.5f, // Vertical motion too
                Mathf.Cos(time * 2f) * motionRadius
            );
            
            Vector3 newPosition = targetPosition + offset;
            waveObject.transform.position = newPosition;
            
            // Update Rigidbody velocity for SphereWaterInteraction
            if (waveRigidbody != null)
            {
                Vector3 velocity = (newPosition - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
                waveRigidbody.linearVelocity = velocity;
            }
            
            previousPosition = newPosition;
        }
        else if (isWaveActive)
        {
            // For one-time waves, create a pulse motion
            float elapsed = Time.time - waveStartTime;
            float pulseSpeed = 5f;
            float motionAmount = Mathf.Sin(elapsed * pulseSpeed) * 0.2f * (1f - elapsed / waveDuration);
            
            Vector3 offset = Vector3.up * motionAmount;
            Vector3 newPosition = targetPosition + offset;
            waveObject.transform.position = newPosition;
            
            // Update Rigidbody velocity
            if (waveRigidbody != null)
            {
                Vector3 velocity = (newPosition - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
                waveRigidbody.linearVelocity = velocity;
            }
            
            previousPosition = newPosition;
            
            // Check if one-time wave duration has passed
            if (elapsed >= waveDuration)
            {
                DeactivateWave();
            }
        }
        else
        {
            // No active waves, just follow position
            waveObject.transform.position = targetPosition;
            previousPosition = targetPosition;
            if (waveRigidbody != null)
            {
                waveRigidbody.linearVelocity = Vector3.zero;
            }
        }
    }
    
    /// <summary>
    /// Creates the wave GameObject with SphereWaterInteraction component
    /// </summary>
    void CreateWaveObject()
    {
        // Use position reference if provided, otherwise use this GameObject's position
        Vector3 startPosition = positionReference != null ? 
            positionReference.transform.position : transform.position;
        
        // Create wave GameObject
        waveObject = new GameObject("WaveGenerator_" + gameObject.name);
        waveObject.transform.position = startPosition;
        previousPosition = startPosition;
        
        // Add Rigidbody for velocity tracking (SphereWaterInteraction needs movement)
        waveRigidbody = waveObject.AddComponent<Rigidbody>();
        waveRigidbody.isKinematic = true; // Don't let physics move it
        waveRigidbody.useGravity = false;
        waveRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Add SphereWaterInteraction component (Crest's built-in wave generator)
        waveInteraction = waveObject.AddComponent<SphereWaterInteraction>();
        
        // Configure wave properties
        if (waveInteraction != null)
        {
            SetWaveProperties();
            waveInteraction.enabled = true;
        }
        
        // Initially disable if not continuous
        if (!continuousWaves)
        {
            waveObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Sets wave properties using reflection (Crest's SphereWaterInteraction properties)
    /// </summary>
    void SetWaveProperties()
    {
        if (waveInteraction == null) return;
        
        // Try to set radius and weight using reflection
        var radiusField = typeof(SphereWaterInteraction).GetField("_Radius", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (radiusField != null)
        {
            radiusField.SetValue(waveInteraction, waveRadius);
        }
        
        var weightField = typeof(SphereWaterInteraction).GetField("_Weight", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (weightField != null)
        {
            weightField.SetValue(waveInteraction, waveIntensity);
        }
        
        // Try using public properties if available
        try
        {
            var radiusProp = typeof(SphereWaterInteraction).GetProperty("Radius");
            if (radiusProp != null && radiusProp.CanWrite)
            {
                radiusProp.SetValue(waveInteraction, waveRadius);
            }
            
            var weightProp = typeof(SphereWaterInteraction).GetProperty("Weight");
            if (weightProp != null && weightProp.CanWrite)
            {
                weightProp.SetValue(waveInteraction, waveIntensity);
            }
        }
        catch
        {
            // Properties might not exist, that's okay
        }
    }
    
    /// <summary>
    /// Activate wave generation (public method for triggering waves)
    /// </summary>
    public void ActivateWave()
    {
        if (waveObject == null)
        {
            CreateWaveObject();
        }
        
        if (waveObject != null && waveInteraction != null)
        {
            waveObject.SetActive(true);
            waveInteraction.enabled = true;
            isWaveActive = true;
            waveStartTime = Time.time;
            
            // Reset position tracking
            Vector3 currentPos = positionReference != null ? 
                positionReference.transform.position : transform.position;
            previousPosition = currentPos;
            waveObject.transform.position = currentPos;
            
            SetWaveProperties();
        }
    }
    
    /// <summary>
    /// Deactivate wave generation
    /// </summary>
    public void DeactivateWave()
    {
        if (waveObject != null)
        {
            isWaveActive = false;
            if (waveInteraction != null)
            {
                waveInteraction.enabled = false;
            }
            if (waveRigidbody != null)
            {
                waveRigidbody.linearVelocity = Vector3.zero;
            }
            // Don't deactivate GameObject, just stop movement
        }
    }
    
    /// <summary>
    /// Generate a single wave pulse at the current position
    /// </summary>
    public void GenerateWave()
    {
        GenerateWave(waveDuration);
    }
    
    /// <summary>
    /// Generate a single wave pulse with custom duration
    /// </summary>
    public void GenerateWave(float duration)
    {
        ActivateWave();
        waveDuration = duration;
        isWaveActive = true;
        waveStartTime = Time.time;
    }
    
    /// <summary>
    /// Update wave properties at runtime
    /// </summary>
    public void UpdateWaveProperties(float radius, float intensity)
    {
        waveRadius = radius;
        waveIntensity = intensity;
        SetWaveProperties();
    }
    
    void OnValidate()
    {
        // Update properties when changed in inspector
        if (waveInteraction != null)
        {
            SetWaveProperties();
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Vector3 position = positionReference != null ? 
            positionReference.transform.position : transform.position;
        
        // Draw wave radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, waveRadius);
        
        // Draw center point
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(position, 0.2f);
    }
    
    void OnDestroy()
    {
        if (waveObject != null)
        {
            Destroy(waveObject);
        }
    }
    
    void OnDisable()
    {
        DeactivateWave();
    }
}
