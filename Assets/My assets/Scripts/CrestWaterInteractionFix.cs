using UnityEngine;
using WaveHarmonic.Crest;
using WaveHarmonic.Crest.Internal;
using System.Reflection;

/// <summary>
/// AGGRESSIVE fix for Crest SphereWaterInteraction stopping - uses reflection to fix internal state
/// This directly manipulates the component's private fields to ensure it keeps working
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereWaterInteraction))]
public class CrestWaterInteractionFix : MonoBehaviour
{
    [Header("Critical Settings")]
    [Tooltip("Force Rigidbody to stay awake - CRITICAL")]
    public bool keepRigidbodyAwake = true;
    
    [Tooltip("Force component to stay enabled")]
    public bool keepComponentEnabled = true;
    
    [Tooltip("Use reflection to force fix internal state (aggressive)")]
    public bool useReflectionFix = true;
    
    [Tooltip("Force minimum velocity even when stationary")]
    public bool forceMinimumVelocity = true;
    
    [Tooltip("Minimum velocity to maintain (m/s)")]
    public float minVelocity = 0.01f;
    
    [Header("Debug")]
    public bool showDebug = false;
    
    private Rigidbody rb;
    private SphereWaterInteraction sphereInteraction;
    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private float lastDeltaTime;
    
    // Reflection fields to access private members
    private FieldInfo previousPositionField;
    private FieldInfo velocityField;
    private FieldInfo velocityClampedField;
    private bool reflectionInitialized;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        sphereInteraction = GetComponent<SphereWaterInteraction>();
        
        if (rb == null || sphereInteraction == null)
        {
            Debug.LogError($"CrestWaterInteractionFix: Missing required components on {gameObject.name}!");
            enabled = false;
            return;
        }
        
        ConfigureRigidbody();
        InitializeReflection();
        
        lastPosition = transform.position;
        lastVelocity = Vector3.zero;
        lastDeltaTime = Time.deltaTime;
        
        if (showDebug) Debug.Log($"CrestWaterInteractionFix: Initialized on {gameObject.name}");
    }
    
    void InitializeReflection()
    {
        if (!useReflectionFix) return;
        
        try
        {
            var type = typeof(SphereWaterInteraction);
            
            // Get private fields using reflection
            previousPositionField = type.GetField("_PreviousPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            velocityField = type.GetField("_Velocity", BindingFlags.NonPublic | BindingFlags.Instance);
            velocityClampedField = type.GetField("_VelocityClamped", BindingFlags.NonPublic | BindingFlags.Instance);
            
            reflectionInitialized = previousPositionField != null && velocityField != null && velocityClampedField != null;
            
            if (showDebug && reflectionInitialized)
            {
                Debug.Log($"CrestWaterInteractionFix: Reflection initialized successfully on {gameObject.name}");
            }
            else if (showDebug)
            {
                Debug.LogWarning($"CrestWaterInteractionFix: Failed to initialize reflection on {gameObject.name}");
            }
        }
        catch (System.Exception e)
        {
            if (showDebug) Debug.LogError($"CrestWaterInteractionFix: Reflection error: {e.Message}");
            reflectionInitialized = false;
        }
    }
    
    void ConfigureRigidbody()
    {
        if (rb == null) return;
        
        rb.sleepThreshold = 0.0001f;
        if (rb.interpolation == RigidbodyInterpolation.None)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        rb.WakeUp();
    }
    
    void Update()
    {
        if (rb == null || sphereInteraction == null) return;
        
        // Keep component enabled
        if (keepComponentEnabled && !sphereInteraction.enabled)
        {
            sphereInteraction.enabled = true;
            if (showDebug) Debug.LogWarning($"CrestWaterInteractionFix: Re-enabled component on {gameObject.name}");
        }
        
        // Keep Rigidbody awake
        if (keepRigidbodyAwake && rb.IsSleeping())
        {
            rb.WakeUp();
        }
        
        // Use reflection to fix internal state
        if (useReflectionFix && reflectionInitialized)
        {
            FixInternalState();
        }
        
        // Track position and velocity
        Vector3 currentPosition = transform.position;
        Vector3 currentVelocity = rb.linearVelocity;
        
        // If object is moving but velocity is zero, force it
        if (forceMinimumVelocity && currentVelocity.magnitude < minVelocity)
        {
            Vector3 positionDelta = currentPosition - lastPosition;
            if (positionDelta.magnitude > 0.001f)
            {
                // Object moved but velocity is zero - force velocity
                Vector3 forcedVelocity = positionDelta / Mathf.Max(Time.deltaTime, 0.0001f);
                forcedVelocity = Vector3.ClampMagnitude(forcedVelocity, minVelocity);
                forcedVelocity.y = currentVelocity.y; // Preserve Y
                
                rb.linearVelocity = forcedVelocity;
                
                if (showDebug && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"CrestWaterInteractionFix: Forced velocity on {gameObject.name}: {forcedVelocity.magnitude:F4} m/s");
                }
            }
        }
        
        lastPosition = currentPosition;
        lastVelocity = currentVelocity;
        lastDeltaTime = Time.deltaTime;
    }
    
    void FixInternalState()
    {
        if (!reflectionInitialized || sphereInteraction == null) return;
        
        try
        {
            Vector3 currentPosition = transform.position;
            
            // Get current values
            Vector3 previousPosition = (Vector3)previousPositionField.GetValue(sphereInteraction);
            Vector3 velocity = (Vector3)velocityField.GetValue(sphereInteraction);
            Vector3 velocityClamped = (Vector3)velocityClampedField.GetValue(sphereInteraction);
            
            // Check if _PreviousPosition is stale (hasn't updated)
            float positionDelta = Vector3.Distance(currentPosition, previousPosition);
            float timeSinceUpdate = Time.time - lastDeltaTime;
            
            // If position changed significantly but velocity is zero, force update
            if (positionDelta > 0.01f && velocity.magnitude < 0.001f)
            {
                // Force update _PreviousPosition to current position minus a small movement
                // This ensures velocity calculation works next frame
                Vector3 forcedPreviousPos = currentPosition - rb.linearVelocity * Time.deltaTime;
                previousPositionField.SetValue(sphereInteraction, forcedPreviousPos);
                
                // Also force velocity to match Rigidbody velocity
                Vector3 rbVelocity = rb.linearVelocity;
                if (rbVelocity.magnitude > 0.001f)
                {
                    velocityField.SetValue(sphereInteraction, rbVelocity);
                    velocityClampedField.SetValue(sphereInteraction, rbVelocity);
                }
                
                if (showDebug && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"CrestWaterInteractionFix: Fixed internal state on {gameObject.name} - Position delta: {positionDelta:F4}");
                }
            }
            // If _PreviousPosition is exactly the same as current (stuck), force a small offset
            else if (positionDelta < 0.0001f && rb.linearVelocity.magnitude > 0.001f)
            {
                // Object is moving but _PreviousPosition is stuck - force it to update
                Vector3 forcedPreviousPos = currentPosition - rb.linearVelocity * Time.deltaTime;
                previousPositionField.SetValue(sphereInteraction, forcedPreviousPos);
            }
        }
        catch (System.Exception e)
        {
            if (showDebug) Debug.LogError($"CrestWaterInteractionFix: Reflection fix error: {e.Message}");
        }
    }
    
    void FixedUpdate()
    {
        if (rb == null) return;
        
        // CONSTANTLY wake Rigidbody
        if (keepRigidbodyAwake && rb.IsSleeping())
        {
            rb.WakeUp();
        }
        
        // Apply tiny force to ensure physics is active
        if (rb.linearVelocity.magnitude < 0.001f && forceMinimumVelocity)
        {
            // Apply a tiny random force to prevent complete stillness
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
        if (rb == null) return;
        
        // Final wake check
        if (keepRigidbodyAwake && rb.IsSleeping())
        {
            rb.WakeUp();
        }
        
        // Force component enabled one more time
        if (sphereInteraction != null && keepComponentEnabled && !sphereInteraction.enabled)
        {
            sphereInteraction.enabled = true;
        }
    }
    
    void OnEnable()
    {
        if (rb != null)
        {
            ConfigureRigidbody();
            rb.WakeUp();
        }
        
        if (sphereInteraction != null && keepComponentEnabled)
        {
            sphereInteraction.enabled = true;
        }
    }
    
    /// <summary>
    /// Force complete re-initialization
    /// </summary>
    public void ForceReinit()
    {
        if (sphereInteraction != null)
        {
            sphereInteraction.enabled = false;
            sphereInteraction.enabled = true;
            
            // Reset reflection state
            if (useReflectionFix && reflectionInitialized)
            {
                try
                {
                    previousPositionField.SetValue(sphereInteraction, transform.position);
                    velocityField.SetValue(sphereInteraction, rb.linearVelocity);
                    velocityClampedField.SetValue(sphereInteraction, rb.linearVelocity);
                }
                catch { }
            }
            
            if (showDebug) Debug.Log($"CrestWaterInteractionFix: Force re-initialized on {gameObject.name}");
        }
        
        if (rb != null)
        {
            rb.WakeUp();
            ConfigureRigidbody();
        }
    }
}
