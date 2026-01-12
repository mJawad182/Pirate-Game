using UnityEngine;
using WaveHarmonic.Crest;
using System.Reflection;

/// <summary>
/// Proper ship wake controller following Crest best practices
/// Uses multiple SphereWaterInteraction points for continuous wake generation
/// Based on Crest manual Section 11.3 Dynamic Waves
/// INCLUDES aggressive fixes to prevent wake from stopping
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShipWakeController : MonoBehaviour
{
    [Header("Wake Configuration")]
    [Tooltip("Front wake point (at ship bow)")]
    public Transform frontWakePoint;
    
    [Tooltip("Back wake point (at ship stern)")]
    public Transform backWakePoint;
    
    [Tooltip("Create wake points automatically if not assigned")]
    public bool autoCreateWakePoints = true;
    
    [Header("Wake Settings")]
    [Tooltip("Radius of wake spheres")]
    [Range(0.5f, 10f)]
    public float wakeRadius = 3f;
    
    [Tooltip("Weight/intensity of wake forces")]
    [Range(1f, 20f)]
    public float wakeWeight = 6f;
    
    [Tooltip("Distance from ship center to front wake point")]
    public float frontDistance = 5f;
    
    [Tooltip("Distance from ship center to back wake point")]
    public float backDistance = -3f;
    
    [Header("Velocity Settings")]
    [Tooltip("Minimum speed to generate wake (m/s)")]
    public float minWakeSpeed = 0.5f;
    
    [Tooltip("Use Rigidbody velocity for wake calculation")]
    public bool useRigidbodyVelocity = true;
    
    [Header("Aggressive Fixes")]
    [Tooltip("Use reflection to fix internal state (prevents wake from stopping)")]
    public bool useReflectionFix = true;
    
    [Tooltip("Force minimum velocity to prevent wake stopping")]
    public bool forceMinimumVelocity = true;
    
    [Tooltip("Check interval for fixing internal state (seconds)")]
    public float fixCheckInterval = 0.5f;
    
    [Header("Debug")]
    public bool showDebug = false;
    
    private Rigidbody rb;
    private SphereWaterInteraction frontSphere;
    private SphereWaterInteraction backSphere;
    private GameObject frontWakeObject;
    private GameObject backWakeObject;
    private Vector3 lastPosition;
    private float timeSinceLastFix;
    
    // Reflection fields for fixing internal state
    private FieldInfo previousPositionField;
    private FieldInfo velocityField;
    private FieldInfo velocityClampedField;
    private bool reflectionInitialized;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("ShipWakeController: Requires Rigidbody component!");
            enabled = false;
            return;
        }
        
        // Initialize reflection for fixing internal state
        InitializeReflection();
        
        // Configure Rigidbody for wake generation
        ConfigureRigidbody();
        
        // Create or find wake points
        SetupWakePoints();
        
        lastPosition = transform.position;
        timeSinceLastFix = 0f;
    }
    
    void InitializeReflection()
    {
        if (!useReflectionFix) return;
        
        try
        {
            var type = typeof(SphereWaterInteraction);
            previousPositionField = type.GetField("_PreviousPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            velocityField = type.GetField("_Velocity", BindingFlags.NonPublic | BindingFlags.Instance);
            velocityClampedField = type.GetField("_VelocityClamped", BindingFlags.NonPublic | BindingFlags.Instance);
            
            reflectionInitialized = previousPositionField != null && velocityField != null && velocityClampedField != null;
            
            if (showDebug && reflectionInitialized)
            {
                Debug.Log($"ShipWakeController: Reflection initialized on {gameObject.name}");
            }
        }
        catch (System.Exception e)
        {
            if (showDebug) Debug.LogError($"ShipWakeController: Reflection init error: {e.Message}");
            reflectionInitialized = false;
        }
    }
    
    void ConfigureRigidbody()
    {
        // CRITICAL: Very low sleep threshold to prevent sleep
        rb.sleepThreshold = 0.0001f;
        
        // Interpolation for smooth movement
        if (rb.interpolation == RigidbodyInterpolation.None)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        
        // Wake up immediately
        rb.WakeUp();
    }
    
    void SetupWakePoints()
    {
        // Create front wake point if needed
        if (frontWakePoint == null && autoCreateWakePoints)
        {
            frontWakeObject = new GameObject("FrontWakePoint");
            frontWakeObject.transform.SetParent(transform);
            frontWakeObject.transform.localPosition = new Vector3(0f, 0f, frontDistance);
            frontWakePoint = frontWakeObject.transform;
        }
        
        // Create back wake point if needed
        if (backWakePoint == null && autoCreateWakePoints)
        {
            backWakeObject = new GameObject("BackWakePoint");
            backWakeObject.transform.SetParent(transform);
            backWakeObject.transform.localPosition = new Vector3(0f, 0f, backDistance);
            backWakePoint = backWakeObject.transform;
        }
        
        // Add SphereWaterInteraction to front point
        if (frontWakePoint != null)
        {
            // Add Rigidbody to wake point for better tracking
            Rigidbody frontRb = frontWakePoint.GetComponent<Rigidbody>();
            if (frontRb == null)
            {
                frontRb = frontWakePoint.gameObject.AddComponent<Rigidbody>();
                frontRb.isKinematic = true; // Kinematic so it follows parent
                frontRb.useGravity = false;
                frontRb.sleepThreshold = 0.0001f;
            }
            
            frontSphere = frontWakePoint.GetComponent<SphereWaterInteraction>();
            if (frontSphere == null)
            {
                frontSphere = frontWakePoint.gameObject.AddComponent<SphereWaterInteraction>();
            }
            
            // Configure front sphere (stronger, creates bow wave)
            SetSphereProperties(frontSphere, wakeRadius, wakeWeight * 1.2f);
        }
        
        // Add SphereWaterInteraction to back point
        if (backWakePoint != null)
        {
            // Add Rigidbody to wake point for better tracking
            Rigidbody backRb = backWakePoint.GetComponent<Rigidbody>();
            if (backRb == null)
            {
                backRb = backWakePoint.gameObject.AddComponent<Rigidbody>();
                backRb.isKinematic = true; // Kinematic so it follows parent
                backRb.useGravity = false;
                backRb.sleepThreshold = 0.0001f;
            }
            
            backSphere = backWakePoint.GetComponent<SphereWaterInteraction>();
            if (backSphere == null)
            {
                backSphere = backWakePoint.gameObject.AddComponent<SphereWaterInteraction>();
            }
            
            // Configure back sphere (creates wake trail)
            SetSphereProperties(backSphere, wakeRadius * 0.8f, wakeWeight);
        }
    }
    
    void SetSphereProperties(SphereWaterInteraction sphere, float radius, float weight)
    {
        // Use reflection to set private fields if needed
        var radiusField = typeof(SphereWaterInteraction).GetField("_Radius", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var weightField = typeof(SphereWaterInteraction).GetField("_Weight", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (radiusField != null) radiusField.SetValue(sphere, radius);
        if (weightField != null) weightField.SetValue(sphere, weight);
        
        // Ensure component is enabled
        sphere.enabled = true;
    }
    
    void Update()
    {
        // Keep Rigidbody awake - CRITICAL for continuous wake
        if (rb.IsSleeping())
        {
            rb.WakeUp();
            if (showDebug) Debug.Log($"ShipWakeController: Woke up Rigidbody on {gameObject.name}");
        }
        
        // CONSTANTLY ensure wake spheres are enabled (they keep getting disabled!)
        if (frontSphere != null)
        {
            if (!frontSphere.enabled)
            {
                frontSphere.enabled = true;
                if (showDebug) Debug.LogWarning($"ShipWakeController: Re-enabled front sphere on {gameObject.name}");
            }
        }
        
        if (backSphere != null)
        {
            if (!backSphere.enabled) 
            {
                backSphere.enabled = true;
                if (showDebug) Debug.LogWarning($"ShipWakeController: Re-enabled back sphere on {gameObject.name}");
            }
        }
        
        // Check if ship is moving fast enough for wake
        Vector3 currentPosition = transform.position;
        float speed = useRigidbodyVelocity ? rb.linearVelocity.magnitude : 
                     Vector3.Distance(currentPosition, lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        
        // Always keep wake enabled if moving (don't disable based on speed - let it fade naturally)
        bool shouldHaveWake = speed > minWakeSpeed;
        
        // Periodically fix internal state using reflection
        timeSinceLastFix += Time.deltaTime;
        if (timeSinceLastFix >= fixCheckInterval && useReflectionFix && reflectionInitialized)
        {
            FixWakeSphereState(frontSphere, frontWakePoint != null ? frontWakePoint.position : currentPosition);
            FixWakeSphereState(backSphere, backWakePoint != null ? backWakePoint.position : currentPosition);
            timeSinceLastFix = 0f;
        }
        
        lastPosition = currentPosition;
    }
    
    void FixWakeSphereState(SphereWaterInteraction sphere, Vector3 spherePosition)
    {
        if (sphere == null || !reflectionInitialized) return;
        
        try
        {
            Vector3 previousPosition = (Vector3)previousPositionField.GetValue(sphere);
            Vector3 velocity = (Vector3)velocityField.GetValue(sphere);
            
            float positionDelta = Vector3.Distance(spherePosition, previousPosition);
            
            // If position changed but velocity is zero, force fix
            if (positionDelta > 0.01f && velocity.magnitude < 0.001f)
            {
                // Force update _PreviousPosition
                Vector3 forcedPreviousPos = spherePosition - rb.linearVelocity * Time.deltaTime;
                previousPositionField.SetValue(sphere, forcedPreviousPos);
                
                // Force velocity to match Rigidbody
                Vector3 rbVelocity = rb.linearVelocity;
                if (rbVelocity.magnitude > 0.001f)
                {
                    velocityField.SetValue(sphere, rbVelocity);
                    velocityClampedField.SetValue(sphere, rbVelocity);
                }
                
                if (showDebug && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"ShipWakeController: Fixed wake sphere state - Position delta: {positionDelta:F4}");
                }
            }
            // If _PreviousPosition is stuck (same as current)
            else if (positionDelta < 0.0001f && rb.linearVelocity.magnitude > 0.001f)
            {
                Vector3 forcedPreviousPos = spherePosition - rb.linearVelocity * Time.deltaTime;
                previousPositionField.SetValue(sphere, forcedPreviousPos);
            }
        }
        catch (System.Exception e)
        {
            if (showDebug) Debug.LogError($"ShipWakeController: Fix state error: {e.Message}");
        }
    }
    
    void FixedUpdate()
    {
        // CONSTANTLY ensure Rigidbody stays awake
        if (rb.IsSleeping())
        {
            rb.WakeUp();
        }
        
        // If using transform movement, sync Rigidbody velocity
        if (!useRigidbodyVelocity)
        {
            Vector3 positionDelta = transform.position - lastPosition;
            if (positionDelta.magnitude > 0.001f)
            {
                Vector3 targetVelocity = positionDelta / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
                targetVelocity.y = rb.linearVelocity.y; // Preserve Y
                rb.linearVelocity = targetVelocity;
            }
        }
        
        // Force minimum velocity to prevent wake stopping
        if (forceMinimumVelocity && rb.linearVelocity.magnitude < 0.001f)
        {
            // Apply tiny force to keep physics active
            Vector3 tinyForce = new Vector3(
                Random.Range(-0.0001f, 0.0001f),
                0f,
                Random.Range(-0.0001f, 0.0001f)
            );
            rb.AddForce(tinyForce, ForceMode.VelocityChange);
        }
    }
    
    void LateUpdate()
    {
        // Final check - ensure spheres stay enabled
        if (frontSphere != null && !frontSphere.enabled)
        {
            frontSphere.enabled = true;
        }
        
        if (backSphere != null && !backSphere.enabled)
        {
            backSphere.enabled = true;
        }
        
        // Final Rigidbody wake check
        if (rb != null && rb.IsSleeping())
        {
            rb.WakeUp();
        }
    }
    
    void OnDisable()
    {
        // Clean up created objects
        if (frontWakeObject != null) Destroy(frontWakeObject);
        if (backWakeObject != null) Destroy(backWakeObject);
    }
    
    /// <summary>
    /// Manually set wake intensity (useful for different ship speeds)
    /// </summary>
    public void SetWakeIntensity(float intensity)
    {
        float newWeight = wakeWeight * intensity;
        
        if (frontSphere != null)
        {
            var weightField = typeof(SphereWaterInteraction).GetField("_Weight", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (weightField != null) weightField.SetValue(frontSphere, newWeight * 1.2f);
        }
        
        if (backSphere != null)
        {
            var weightField = typeof(SphereWaterInteraction).GetField("_Weight", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (weightField != null) weightField.SetValue(backSphere, newWeight);
        }
    }
}
