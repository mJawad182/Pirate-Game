using UnityEngine;
using System.Collections;
using WaveHarmonic.Crest;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;
    
    // Event fired when parrot is destroyed
    public static System.Action OnParrotDestroyed;
    public static System.Action OnEnemyShip1Arrive;
    public static System.Action OnEnemyShip2Arrive;
    public static System.Action OnEnemyShip3Arrive;
    public static System.Action OnEnemyShip4Arrive;
    
    // Event fired to start weather system
    public static System.Action OnStartWeatherSystem;
    
    // Event fired when cannon is fired (passes firing position)
    public static System.Action<Vector3> OnCannonFired;
    
    // Event fired when cannon bullet hits (passes hit position)
    public static System.Action<Vector3> OnCannonHit;
    
    [Header("Crest Water Renderer Camera Switch")]
    [Tooltip("Second camera to switch to (assign in Inspector)")]
    public Camera secondCamera;
    
    [Tooltip("Delay in seconds after first ship arrives before switching camera")]
    [Range(0f, 60f)]
    public float cameraSwitchDelay = 20f;
    
    [Header("Weather System Delay")]
    [Tooltip("Delay in seconds after camera switch before starting weather system")]
    [Range(0f, 60f)]
    public float weatherSystemDelay = 10f;
    
    [Tooltip("Crest WaterRenderer component (will auto-find if not assigned)")]
    public WaterRenderer waterRenderer;
    
    [Tooltip("Auto-find WaterRenderer if not assigned")] 
    public bool autoFindWaterRenderer = true;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private bool weatherSystemCountdownStarted = false;
    private bool cameraSwitchStarted = false;
    private Camera mainCamera;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Find main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[EventManager] Main camera not found! Camera switching may not work properly.");
        }
        
        // Find WaterRenderer if not assigned
        if (waterRenderer == null && autoFindWaterRenderer)
        {
            waterRenderer = FindObjectOfType<WaterRenderer>();
            if (waterRenderer != null && showDebug)
            {
                Debug.Log($"[EventManager] Auto-found WaterRenderer: {waterRenderer.name}");
            }
        }
        
        if (waterRenderer == null)
        {
            Debug.LogWarning("[EventManager] WaterRenderer not found! Camera switching will not work. Please assign it in the Inspector.");
        }
        
        if (secondCamera == null)
        {
            Debug.LogWarning("[EventManager] Second camera not assigned! Camera switching will not work. Please assign it in the Inspector.");
        }
        
        // Subscribe to all ship arrival events to start weather countdown
        OnEnemyShip1Arrive += OnAnyShipArrive;
        OnEnemyShip2Arrive += OnAnyShipArrive;
        OnEnemyShip3Arrive += OnAnyShipArrive;
        OnEnemyShip4Arrive += OnAnyShipArrive;
        
        if (showDebug) Debug.Log("[EventManager] Subscribed to all ship arrival events for weather system countdown");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from ship arrival events
        OnEnemyShip1Arrive -= OnAnyShipArrive;
        OnEnemyShip2Arrive -= OnAnyShipArrive;
        OnEnemyShip3Arrive -= OnAnyShipArrive;
        OnEnemyShip4Arrive -= OnAnyShipArrive;
    }
    
    /// <summary>
    /// Called when any ship arrives - starts camera switch countdown
    /// </summary>
    private void OnAnyShipArrive()
    {
        // Start camera switch countdown (only once)
        if (!cameraSwitchStarted)
        {
            cameraSwitchStarted = true;
            if (showDebug) Debug.Log($"[EventManager] First ship arrived! Starting {cameraSwitchDelay} second countdown to camera switch...");
            StartCoroutine(CameraSwitchCountdown());
        }
    }
    
    /// <summary>
    /// Coroutine that waits for the camera switch delay, then switches the camera and starts weather countdown
    /// </summary>
    private IEnumerator CameraSwitchCountdown()
    {
        yield return new WaitForSeconds(cameraSwitchDelay);
        
        if (showDebug) Debug.Log($"[EventManager] {cameraSwitchDelay} seconds elapsed. Switching Crest WaterRenderer camera!");
        SwitchWaterRendererCamera();
        
        // Start weather system countdown after camera switch
        if (!weatherSystemCountdownStarted)
        {
            weatherSystemCountdownStarted = true;
            if (showDebug) Debug.Log($"[EventManager] Starting {weatherSystemDelay} second countdown to weather system (after camera switch)...");
            StartCoroutine(WeatherSystemCountdown());
        }
    }
    
    /// <summary>
    /// Switches Crest WaterRenderer's Viewpoint from main camera to second camera
    /// </summary>
    private void SwitchWaterRendererCamera()
    {
        if (waterRenderer == null)
        {
            if (showDebug) Debug.LogWarning("[EventManager] Cannot switch camera: WaterRenderer not found!");
            return;
        }
        
        if (secondCamera == null)
        {
            if (showDebug) Debug.LogWarning("[EventManager] Cannot switch camera: Second camera not assigned!");
            return;
        }
        
        // Store current viewpoint for debug
        Transform currentViewpoint = waterRenderer.Viewpoint;
        string currentViewpointName = currentViewpoint != null ? currentViewpoint.name : "null";
        
        // Switch to second camera
        waterRenderer.Viewpoint = secondCamera.transform;
        
        if (showDebug)
        {
            Debug.Log($"[EventManager] Switched Crest WaterRenderer Viewpoint from '{currentViewpointName}' to '{secondCamera.name}'");
        }
    }
    
    /// <summary>
    /// Coroutine that waits for the delay, then starts the weather system
    /// </summary>
    private IEnumerator WeatherSystemCountdown()
    {
        yield return new WaitForSeconds(weatherSystemDelay);
        
        if (showDebug) Debug.Log($"[EventManager] {weatherSystemDelay} seconds elapsed. Starting weather system!");
        StartWeatherSystem();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnParrotDestroyedApplier(){
        OnParrotDestroyed?.Invoke();
    }
    
    public void StartWeatherSystem(){
        OnStartWeatherSystem?.Invoke();
    }

    public void OnEnemyShip1ArriveApplier(){
        OnEnemyShip1Arrive?.Invoke();
    }

    public void OnEnemyShip2ArriveApplier(){
        OnEnemyShip2Arrive?.Invoke();
    }
    
    public void OnEnemyShip3ArriveApplier(){
        OnEnemyShip3Arrive?.Invoke();
    }
    
    public void OnEnemyShip4ArriveApplier(){
        OnEnemyShip4Arrive?.Invoke();
    }
}
