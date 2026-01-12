using UnityEngine;
using WaveHarmonic.Crest;
using System.Reflection;

/// <summary>
/// Custom wave generator using DynamicWavesLodInput with procedural geometry
/// Completely bypasses SphereWaterInteraction - uses Crest's Renderer input mode
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DirectWaveGenerator : MonoBehaviour
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
    
    [Header("Advanced")]
    [Tooltip("Use Rigidbody velocity")]
    public bool useRigidbodyVelocity = true;
    
    private Rigidbody rb;
    private GameObject frontObject;
    private GameObject backObject;
    private DynamicWavesLodInput frontInput;
    private DynamicWavesLodInput backInput;
    private MeshRenderer frontRenderer;
    private MeshRenderer backRenderer;
    private Mesh frontMesh;
    private Mesh backMesh;
    private Material waveMaterial;
    private Vector3 lastPosition;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("DirectWaveGenerator: Requires Rigidbody!");
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
        
        // Create wave material (uses Crest's Dynamic Waves shader)
        CreateWaveMaterial();
        
        // Create wake points and inputs
        CreateWakeInputs();
        
        lastPosition = transform.position;
    }
    
    void CreateWaveMaterial()
    {
        // Use Crest's Dynamic Waves Add Bump shader
        Shader waveShader = Shader.Find("Crest/Inputs/Dynamic Waves/Add Bump");
        if (waveShader == null)
        {
            Debug.LogError("DirectWaveGenerator: Could not find Crest Dynamic Waves shader! Make sure Crest is properly imported.");
            enabled = false;
            return;
        }
        
        waveMaterial = new Material(waveShader);
        waveMaterial.SetFloat("_Crest_Amplitude", waveIntensity);
        waveMaterial.SetFloat("_Crest_Radius", waveRadius);
    }
    
    void CreateWakeInputs()
    {
        // Front point
        if (frontPoint == null)
        {
            frontObject = new GameObject("FrontWaveInput");
            frontObject.transform.SetParent(transform);
            frontObject.transform.localPosition = new Vector3(0f, 0f, frontDistance);
            frontPoint = frontObject.transform;
        }
        
        // Create mesh and renderer for front
        frontMesh = CreateWaveMesh(waveRadius);
        frontRenderer = frontObject.AddComponent<MeshRenderer>();
        frontRenderer.material = waveMaterial;
        frontRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        frontRenderer.receiveShadows = false;
        
        MeshFilter frontFilter = frontObject.AddComponent<MeshFilter>();
        frontFilter.mesh = frontMesh;
        
        // Add DynamicWavesLodInput using extension method to set mode
        frontInput = frontObject.AddComponent<DynamicWavesLodInput>(LodInputMode.Renderer);
        
        // Assign renderer to input data using reflection (required for Renderer mode)
        if (frontInput.Data != null)
        {
            var rendererField = frontInput.Data.GetType().GetField("_Renderer", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererField != null)
            {
                rendererField.SetValue(frontInput.Data, frontRenderer);
            }
        }
        
        // Back point
        if (backPoint == null)
        {
            backObject = new GameObject("BackWaveInput");
            backObject.transform.SetParent(transform);
            backObject.transform.localPosition = new Vector3(0f, 0f, backDistance);
            backPoint = backObject.transform;
        }
        
        // Create mesh and renderer for back
        backMesh = CreateWaveMesh(waveRadius * 0.8f);
        backRenderer = backObject.AddComponent<MeshRenderer>();
        backRenderer.material = waveMaterial;
        backRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        backRenderer.receiveShadows = false;
        
        MeshFilter backFilter = backObject.AddComponent<MeshFilter>();
        backFilter.mesh = backMesh;
        
        // Add DynamicWavesLodInput using extension method to set mode
        backInput = backObject.AddComponent<DynamicWavesLodInput>(LodInputMode.Renderer);
        
        // Assign renderer to input data using reflection (required for Renderer mode)
        if (backInput.Data != null)
        {
            var rendererField = backInput.Data.GetType().GetField("_Renderer", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererField != null)
            {
                rendererField.SetValue(backInput.Data, backRenderer);
            }
        }
    }
    
    Mesh CreateWaveMesh(float radius)
    {
        Mesh mesh = new Mesh();
        mesh.name = "WaveMesh";
        
        // Create a simple quad that covers the wave area
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-radius * 2f, 0f, -radius * 2f),
            new Vector3(radius * 2f, 0f, -radius * 2f),
            new Vector3(-radius * 2f, 0f, radius * 2f),
            new Vector3(radius * 2f, 0f, radius * 2f)
        };
        
        int[] triangles = new int[]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    void Update()
    {
        // Keep Rigidbody awake
        if (rb.IsSleeping())
        {
            rb.WakeUp();
        }
        
        // Calculate speed
        Vector3 currentPosition = transform.position;
        Vector3 velocity = useRigidbodyVelocity ? rb.linearVelocity : 
                          (currentPosition - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        
        float speed = velocity.magnitude;
        
        // Enable/disable based on speed
        bool shouldBeActive = speed > minSpeed;
        
        if (frontInput != null)
        {
            frontInput.enabled = shouldBeActive;
            if (frontRenderer != null) frontRenderer.enabled = shouldBeActive;
        }
        
        if (backInput != null)
        {
            backInput.enabled = shouldBeActive;
            if (backRenderer != null) backRenderer.enabled = shouldBeActive;
        }
        
        // Update material properties based on velocity
        if (waveMaterial != null && shouldBeActive)
        {
            // Update amplitude based on speed
            float speedMultiplier = Mathf.Clamp01(speed / 10f);
            waveMaterial.SetFloat("_Crest_Amplitude", waveIntensity * speedMultiplier);
        }
        
        lastPosition = currentPosition;
    }
    
    void FixedUpdate()
    {
        if (rb.IsSleeping())
        {
            rb.WakeUp();
        }
    }
    
    void OnDisable()
    {
        if (frontObject != null) Destroy(frontObject);
        if (backObject != null) Destroy(backObject);
        if (waveMaterial != null) Destroy(waveMaterial);
        if (frontMesh != null) Destroy(frontMesh);
        if (backMesh != null) Destroy(backMesh);
    }
    
    void OnDestroy()
    {
        OnDisable();
    }
}
