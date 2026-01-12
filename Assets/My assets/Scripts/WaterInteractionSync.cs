using UnityEngine;

/// <summary>
/// Fixes issue where SphereWaterInteraction stops working after movement
/// Ensures Rigidbody stays awake and maintains proper velocity tracking for water interaction
/// Use this on the same GameObject that has Rigidbody, FloatingObject, and SphereWaterInteraction
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WaterInteractionSync : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Keep Rigidbody awake to prevent sleep issues that break water interaction")]
    public bool keepAwake = true;
    
    [Tooltip("Check velocity periodically to ensure it's being tracked")]
    public bool monitorVelocity = true;
    
    [Tooltip("Minimum velocity threshold (m/s) - if consistently below this, force wake")]
    public float minVelocityThreshold = 0.01f;
    
    [Tooltip("Time (seconds) between velocity checks")]
    public float checkInterval = 0.5f;
    
    private Rigidbody rb;
    private Vector3 lastPosition;
    private float timeSinceLastCheck;
    private float consecutiveLowVelocityCount;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
        
        // Ensure Rigidbody is configured properly for water interaction
        if (rb != null)
        {
            // Very low threshold to prevent premature sleep
            rb.sleepThreshold = 0.001f;
            
            // Keep interpolation for smooth movement
            if (rb.interpolation == RigidbodyInterpolation.None)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            
            if (keepAwake)
            {
                rb.WakeUp();
            }
        }
        
        Debug.Log($"WaterInteractionSync: Initialized on {gameObject.name} to maintain water interaction");
    }
    
    void Update()
    {
        if (rb == null) return;
        
        // Periodically check if Rigidbody is awake
        if (keepAwake && rb.IsSleeping())
        {
            rb.WakeUp();
        }
        
        // Monitor velocity tracking periodically
        if (monitorVelocity)
        {
            timeSinceLastCheck += Time.deltaTime;
            
            if (timeSinceLastCheck >= checkInterval)
            {
                CheckVelocityTracking();
                timeSinceLastCheck = 0f;
            }
        }
        
        // Update last position for velocity calculation
        lastPosition = transform.position;
    }
    
    void FixedUpdate()
    {
        if (rb == null) return;
        
        // Constantly ensure Rigidbody stays awake
        // This is critical - when Rigidbody sleeps, SphereWaterInteraction can't track velocity
        if (keepAwake && rb.IsSleeping())
        {
            rb.WakeUp();
        }
    }
    
    void CheckVelocityTracking()
    {
        if (rb == null) return;
        
        // Check if object is moving but velocity is low (indicates tracking issue)
        Vector3 positionChange = transform.position - lastPosition;
        float actualSpeed = positionChange.magnitude / Time.deltaTime;
        float rigidbodySpeed = rb.linearVelocity.magnitude;
        
        // If actual movement is happening but Rigidbody velocity is low, there's an issue
        if (actualSpeed > minVelocityThreshold && rigidbodySpeed < minVelocityThreshold)
        {
            consecutiveLowVelocityCount++;
            
            // Force wake up and reset velocity tracking
            if (consecutiveLowVelocityCount >= 3)
            {
                rb.WakeUp();
                
                // Try to sync velocity with actual movement
                Vector3 targetVelocity = positionChange / Time.fixedDeltaTime;
                targetVelocity.y = rb.linearVelocity.y; // Preserve Y for FloatingObject
                rb.linearVelocity = targetVelocity;
                
                consecutiveLowVelocityCount = 0;
                
                Debug.LogWarning($"WaterInteractionSync: Detected velocity tracking issue on {gameObject.name}. Forced sync.");
            }
        }
        else
        {
            consecutiveLowVelocityCount = 0;
        }
    }
    
    void OnDisable()
    {
        consecutiveLowVelocityCount = 0;
        timeSinceLastCheck = 0f;
    }
}
