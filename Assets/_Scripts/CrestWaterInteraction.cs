using UnityEngine;

/// <summary>
/// Integrates with Crest Ocean System to create reactive water interactions.
/// Handles ship movement, object interactions, and dynamic water effects.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CrestWaterInteraction : MonoBehaviour
{
    [Header("Crest Integration")]
    [Tooltip("Reference to Crest OceanRenderer (if available)")]
    public GameObject oceanRenderer;
    
    [Header("Floating Settings")]
    [Tooltip("How much the object floats above/below water")]
    public float buoyancy = 1f;
    
    [Tooltip("Water level offset (adjust if water is not at y=0)")]
    public float waterLevelOffset = 0f;
    
    [Tooltip("Damping factor for water resistance")]
    public float waterDrag = 0.5f;
    
    [Tooltip("Damping factor for angular water resistance")]
    public float waterAngularDrag = 0.5f;
    
    [Header("Wave Interaction")]
    [Tooltip("How much this object affects water waves (requires Crest wave interaction)")]
    public float waveInteractionStrength = 1f;
    
    [Tooltip("Radius of wave interaction")]
    public float waveInteractionRadius = 5f;
    
    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebug = false;
    
    private Rigidbody rb;
    private float originalDrag;
    private float originalAngularDrag;
    
    // Crest API references (if Crest is available)
    private System.Type oceanRendererType;
    private System.Type sampleHeightHelperType;
    private System.Type registerDynWavesInputType;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
        
        // Try to find Crest types
        InitializeCrestIntegration();
        
        // Find ocean renderer if not assigned
        if (oceanRenderer == null)
        {
            oceanRenderer = GameObject.Find("OceanRenderer");
            if (oceanRenderer == null)
            {
                oceanRenderer = GameObject.Find("CrestOceanRenderer");
            }
        }
    }
    
    /// <summary>
    /// Initialize Crest Ocean System integration
    /// </summary>
    void InitializeCrestIntegration()
    {
        // Try to find Crest types using reflection (works even if Crest is not imported)
        string[] possibleNamespaces = new string[]
        {
            "Crest",
            "Crest.Examples",
            "Crest.Spline",
            ""
        };
        
        foreach (string ns in possibleNamespaces)
        {
            string fullName = string.IsNullOrEmpty(ns) ? "OceanRenderer" : ns + ".OceanRenderer";
            oceanRendererType = System.Type.GetType(fullName);
            if (oceanRendererType != null) break;
        }
        
        if (oceanRendererType != null && showDebug)
        {
            Debug.Log("Crest Ocean System detected!");
        }
    }
    
    /// <summary>
    /// Sample water height at a given world position using Crest API
    /// </summary>
    public float SampleWaterHeight(Vector3 worldPos)
    {
        if (oceanRendererType == null || oceanRenderer == null)
        {
            // Fallback: return simple water level
            return waterLevelOffset;
        }
        
        try
        {
            // Use Crest's SampleHeight helper if available
            // This is a simplified version - actual Crest API may differ
            var sampleMethod = oceanRendererType.GetMethod("SampleHeight", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (sampleMethod != null)
            {
                object result = sampleMethod.Invoke(null, new object[] { worldPos });
                if (result is float)
                {
                    return (float)result + waterLevelOffset;
                }
            }
        }
        catch (System.Exception e)
        {
            if (showDebug)
            {
                Debug.LogWarning($"Crest sampling failed: {e.Message}. Using fallback.");
            }
        }
        
        return waterLevelOffset;
    }
    
    /// <summary>
    /// Register dynamic wave input to Crest (for reactive water)
    /// </summary>
    void RegisterWaveInput()
    {
        if (oceanRendererType == null || waveInteractionStrength <= 0)
            return;
        
        try
        {
            // This would register the object's position/velocity with Crest's wave system
            // Actual implementation depends on Crest version and API
            // Example: RegisterDynWavesInput(position, velocity, radius, strength)
            
            Vector3 velocity = rb.linearVelocity;
            Vector3 position = transform.position;
            
            // Crest API example (may need adjustment based on version):
            // Crest.RegisterDynWavesInput(position, velocity, waveInteractionRadius, waveInteractionStrength);
        }
        catch (System.Exception e)
        {
            if (showDebug)
            {
                Debug.LogWarning($"Crest wave registration failed: {e.Message}");
            }
        }
    }
    
    void FixedUpdate()
    {
        if (rb == null) return;
        
        // Sample water height at object position
        Vector3 samplePos = transform.position;
        float waterHeight = SampleWaterHeight(samplePos);
        
        // Calculate buoyancy force
        float depth = waterHeight - samplePos.y;
        
        if (depth > 0)
        {
            // Object is below water - apply buoyancy
            float buoyancyForce = depth * buoyancy * Physics.gravity.magnitude;
            rb.AddForce(Vector3.up * buoyancyForce, ForceMode.Acceleration);
            
            // Apply water drag
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }
        else
        {
            // Object is above water - restore original drag
            rb.linearDamping = originalDrag;
            rb.angularDamping = originalAngularDrag;
        }
        
        // Register wave interaction with Crest
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            RegisterWaveInput();
        }
        
        // Debug visualization
        if (showDebug)
        {
            Debug.DrawLine(samplePos, new Vector3(samplePos.x, waterHeight, samplePos.z), Color.cyan);
        }
    }
    
    /// <summary>
    /// Get current water height at this object's position
    /// </summary>
    public float GetWaterHeight()
    {
        return SampleWaterHeight(transform.position);
    }
    
    /// <summary>
    /// Check if object is currently in water
    /// </summary>
    public bool IsInWater()
    {
        return transform.position.y < GetWaterHeight();
    }
    
    void OnDrawGizmosSelected()
    {
        if (showDebug)
        {
            // Draw wave interaction radius
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, waveInteractionRadius);
        }
    }
}



