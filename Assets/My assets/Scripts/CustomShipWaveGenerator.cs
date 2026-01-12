using UnityEngine;
using WaveHarmonic.Crest;
using System.Reflection;

/// <summary>
/// Custom wave generator that creates and manages SphereWaterInteraction components
/// This ensures they stay active and don't stop working
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CustomShipWaveGenerator : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("Radius of wave generation")]
    [Range(1f, 20f)]
    public float waveRadius = 5f;
    
    [Tooltip("Intensity/strength of waves")]
    [Range(1f, 30f)]
    public float waveIntensity = 10f;
    
    [Tooltip("Minimum speed to generate waves (m/s)")]
    public float minSpeed = 0.5f;
    
    [Header("Wake Points")]
    [Tooltip("Front wake point (bow) - leave empty to auto-create")]
    public Transform frontPoint;
    
    [Tooltip("Back wake point (stern) - leave empty to auto-create")]
    public Transform backPoint;
    
    [Tooltip("Distance from center to front point")]
    public float frontDistance = 5f;
    
    [Tooltip("Distance from center to back point")]
    public float backDistance = -3f;
    
    [Header("Maintenance")]
    [Tooltip("Force fix internal state using reflection")]
    public bool useReflectionFix = true;
    
    [Tooltip("Check interval for fixes (seconds)")]
    [Range(0.1f, 2f)]
    public float fixInterval = 0.5f;
    
    [Header("Debug")]
    public bool showDebug = false;
    
    private Rigidbody rb;
    private SphereWaterInteraction frontSphere;
    private SphereWaterInteraction backSphere;
    private GameObject frontObject;
    private GameObject backObject;
    private Vector3 lastPosition;
    private float timeSinceFix;
    
    // Reflection fields
    private FieldInfo previousPositionField;
    private FieldInfo velocityField;
    private FieldInfo velocityClampedField;
    private bool reflectionInitialized;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("CustomShipWaveGenerator: Requires Rigidbody!");
            enabled = false;
            return;
        }
        
        // Configure Rigidbody
        rb.sleepThreshold = 0.0001f;
        if (rb.interpolation == RigidbodyInterpolation.None)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        rb.WakeUp();
        
        // Initialize reflection
        InitializeReflection();
        
        // Create wake points and spheres
        CreateWakeSpheres();
        
        lastPosition = transform.position;
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
                Debug.Log("CustomShipWaveGenerator: Reflection initialized");
            }
        }
        catch (System.Exception e)
        {
            if (showDebug) Debug.LogError($"Reflection init error: {e.Message}");
            reflectionInitialized = false;
        }
    }
    
    void CreateWakeSpheres()
    {
        // Front point
        if (frontPoint == null)
        {
            frontObject = new GameObject("FrontWavePoint");
            frontObject.transform.SetParent(transform);
            frontObject.transform.localPosition = new Vector3(0f, 0f, frontDistance);
            frontPoint = frontObject.transform;
        }
        
        // Add Rigidbody to front point
        Rigidbody frontRb = frontPoint.GetComponent<Rigidbody>();
        if (frontRb == null)
        {
            frontRb = frontPoint.gameObject.AddComponent<Rigidbody>();
            frontRb.isKinematic = true;
            frontRb.useGravity = false;
            frontRb.sleepThreshold = 0.0001f;
        }
        
        // Add SphereWaterInteraction
        frontSphere = frontPoint.GetComponent<SphereWaterInteraction>();
        if (frontSphere == null)
        {
            frontSphere = frontPoint.gameObject.AddComponent<SphereWaterInteraction>();
        }
        
        SetSphereProperties(frontSphere, waveRadius, waveIntensity * 1.2f);
        
        // Back point
        if (backPoint == null)
        {
            backObject = new GameObject("BackWavePoint");
            backObject.transform.SetParent(transform);
            backObject.transform.localPosition = new Vector3(0f, 0f, backDistance);
            backPoint = backObject.transform;
        }
        
        // Add Rigidbody to back point
        Rigidbody backRb = backPoint.GetComponent<Rigidbody>();
        if (backRb == null)
        {
            backRb = backPoint.gameObject.AddComponent<Rigidbody>();
            backRb.isKinematic = true;
            backRb.useGravity = false;
            backRb.sleepThreshold = 0.0001f;
        }
        
        // Add SphereWaterInteraction
        backSphere = backPoint.GetComponent<SphereWaterInteraction>();
        if (backSphere == null)
        {
            backSphere = backPoint.gameObject.AddComponent<SphereWaterInteraction>();
        }
        
        SetSphereProperties(backSphere, waveRadius * 0.8f, waveIntensity);
    }
    
    void SetSphereProperties(SphereWaterInteraction sphere, float radius, float weight)
    {
        var radiusField = typeof(SphereWaterInteraction).GetField("_Radius", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var weightField = typeof(SphereWaterInteraction).GetField("_Weight", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (radiusField != null) radiusField.SetValue(sphere, radius);
        if (weightField != null) weightField.SetValue(sphere, weight);
        
        sphere.enabled = true;
    }
    
    void Update()
    {
        // Keep Rigidbody awake
        if (rb.IsSleeping())
        {
            rb.WakeUp();
        }
        
        // CONSTANTLY ensure spheres are enabled (they keep getting disabled!)
        if (frontSphere != null && !frontSphere.enabled)
        {
            frontSphere.enabled = true;
            if (showDebug) Debug.LogWarning("Re-enabled front sphere!");
        }
        
        if (backSphere != null && !backSphere.enabled)
        {
            backSphere.enabled = true;
            if (showDebug) Debug.LogWarning("Re-enabled back sphere!");
        }
        
        // Calculate speed
        Vector3 currentPosition = transform.position;
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        
        // Fix internal state periodically
        timeSinceFix += Time.deltaTime;
        if (timeSinceFix >= fixInterval && useReflectionFix && reflectionInitialized)
        {
            FixSphereState(frontSphere, frontPoint.position, velocity);
            FixSphereState(backSphere, backPoint.position, velocity);
            timeSinceFix = 0f;
        }
        
        lastPosition = currentPosition;
    }
    
    void FixSphereState(SphereWaterInteraction sphere, Vector3 spherePosition, Vector3 shipVelocity)
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
                Vector3 forcedPreviousPos = spherePosition - shipVelocity * Time.deltaTime;
                previousPositionField.SetValue(sphere, forcedPreviousPos);
                
                // Force velocity to match ship velocity
                if (shipVelocity.magnitude > 0.001f)
                {
                    velocityField.SetValue(sphere, shipVelocity);
                    velocityClampedField.SetValue(sphere, shipVelocity);
                }
            }
            // If _PreviousPosition is stuck
            else if (positionDelta < 0.0001f && shipVelocity.magnitude > 0.001f)
            {
                Vector3 forcedPreviousPos = spherePosition - shipVelocity * Time.deltaTime;
                previousPositionField.SetValue(sphere, forcedPreviousPos);
            }
        }
        catch (System.Exception e)
        {
            if (showDebug) Debug.LogError($"Fix state error: {e.Message}");
        }
    }
    
    void FixedUpdate()
    {
        // Keep Rigidbody awake
        if (rb.IsSleeping())
        {
            rb.WakeUp();
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
    }
    
    void OnDisable()
    {
        // Cleanup
        if (frontObject != null) Destroy(frontObject);
        if (backObject != null) Destroy(backObject);
    }
    
    void OnDestroy()
    {
        OnDisable();
    }
}
