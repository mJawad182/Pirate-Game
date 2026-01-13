using UnityEngine;
using System.Collections;
using System.Reflection;
using Enviro;
using THOR;
using WaveHarmonic.Crest;

/// <summary>
/// Controls gradual weather progression from Cloudy 1 → Cloudy 2 → Cloudy 3 → Storm
/// Activates THOR Thunderstorm just before storm weather
/// Moves a Transform GameObject to the player (Main Camera) at configurable speed
/// </summary>
public class WeatherProgressionController : MonoBehaviour
{
    [Header("Weather Progression Settings")]
    [Tooltip("Total duration for all weather transitions (20-25 seconds recommended)")]
    [Range(20f, 300f)]
    public float totalTransitionDuration = 22.5f;

    [Tooltip("Weather type names in Enviro (must match exactly)")]
    public string cloudy1WeatherName = "Cloudy 1";
    public string cloudy2WeatherName = "Cloudy 2";
    public string cloudy3WeatherName = "Cloudy 3";
    public string stormWeatherName = "Storm";

    [Header("Cloud Transition Speed")]
    [Tooltip("Speed of cloud formation/transition (lower = slower, more visible)")]
    [Range(0.1f, 2f)]
    public float cloudTransitionSpeed = 0.5f;

    [Header("THOR Thunderstorm Settings")]
    [Tooltip("Reference to THOR Thunderstorm GameObject (optional - will find automatically if not set)")]
    public GameObject thorThunderstormObject;

    [Tooltip("When to activate THOR Thunderstorm (0 = start, 1 = end, 0.8 = 80% through)")]
    [Range(0f, 1f)]
    public float thorActivationPoint = 0.85f; // Activate at 85% through (just before storm)

    [Tooltip("THOR Thunderstorm intensity when activated (0 = off, 1 = full intensity)")]
    [Range(0f, 1f)]
    public float thorIntensity = 1f;

    [Tooltip("THOR Thunderstorm transition duration")]
    [Range(1f, 10f)]
    public float thorTransitionDuration = 5f;

    [Header("Transform Movement Settings")]
    [Tooltip("Transform GameObject to move towards player")]
    public Transform transformToMove;

    [Tooltip("Speed at which the transform moves towards player (units per second)")]
    [Range(0.1f, 50f)]
    public float movementSpeed = 5f;

    [Tooltip("Target to move towards (Main Camera if not set)")]
    public Transform targetTransform;

    [Tooltip("Stop moving when this close to target")]
    [Range(0.1f, 10f)]
    public float stopDistance = 1f;

    [Header("Wave Intensity Settings")]
    [Tooltip("Starting wave multiplier (0 = calm, 1 = normal, 2+ = intense)")]
    [Range(0f, 1f)]
    public float startingWaveMultiplier = 0f;

    [Tooltip("Maximum wave multiplier during storm (2-5 recommended)")]
    [Range(1f, 8f)]
    public float maxStormWaveMultiplier = 4f;

    [Tooltip("Wave multiplier at Cloudy 2")]
    [Range(0f, 3f)]
    public float cloudy2WaveMultiplier = 0.5f;

    [Tooltip("Wave multiplier at Cloudy 3")]
    [Range(0f, 4f)]
    public float cloudy3WaveMultiplier = 1.5f;

    [Tooltip("Smooth transition speed for wave intensity changes")]
    [Range(0.1f, 5f)]
    public float waveIntensityTransitionSpeed = 1f;
    
    [Tooltip("Wave regeneration interval (seconds) - lower = smoother but more performance cost")]
    [Range(0.1f, 2f)]
    public float waveRegenerationInterval = 0.5f;

    [Header("Debug")]
    [Tooltip("Start weather progression automatically on Start()")]
    public bool startOnStart = true;

    [Tooltip("Delay before starting weather progression (seconds)")]
    [Range(0f, 60f)]
    public float startDelay = 20f;

    [Tooltip("Show debug messages")]
    public bool showDebug = false;

    // Private variables
    private EnviroManager enviroManager;
    private EnviroWeatherModule weatherModule;
    private bool isProgressionActive = false;
    private bool thorActivated = false;
    private Camera mainCamera;
    
    // Crest wave control
    private ShapeGerstner[] shapeGerstnerComponents;
    private WaveSpectrum[] waveSpectra;
    private float currentWaveMultiplier = 0f;
    private float targetWaveMultiplier = 0f;
    private WaterRenderer waterRenderer;
    private float originalWindSpeed = 20f; // Default wind speed
    private float lastRegenerationTime = 0f;
    private float lastMultiplierValue = 0f;

    void Start()
    {
        // Find Enviro Manager
        enviroManager = FindObjectOfType<EnviroManager>();
        if (enviroManager == null)
        {
            Debug.LogError("WeatherProgressionController: EnviroManager not found in scene!");
            enabled = false;
            return;
        }

        // Get Weather Module
        weatherModule = enviroManager.Weather;
        if (weatherModule == null)
        {
            Debug.LogError("WeatherProgressionController: EnviroWeatherModule not found!");
            enabled = false;
            return;
        }

        // Find Main Camera if target not set
        if (targetTransform == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                targetTransform = mainCamera.transform;
            }
            else
            {
                Debug.LogWarning("WeatherProgressionController: Main Camera not found! Set targetTransform manually.");
            }
        }

        // Find THOR Thunderstorm if not assigned
        if (thorThunderstormObject == null)
        {
            // Try to find THOR_Thunderstorm component
            var thorComponent = FindObjectOfType<THOR_Thunderstorm>();
            if (thorComponent != null)
            {
                thorThunderstormObject = thorComponent.gameObject;
                if (showDebug) Debug.Log("WeatherProgressionController: Found THOR Thunderstorm component");
            }
            else
            {
                // Try to find by name
                GameObject thorObj = GameObject.Find("THOR_Thunderstorm");
                if (thorObj == null)
                {
                    thorObj = GameObject.Find("THOR Thunderstorm");
                }
                if (thorObj == null)
                {
                    // Try to find prefab in scene
                    thorObj = GameObject.Find("THOR_Thunderstorm(Clone)");
                }
                if (thorObj != null)
                {
                    thorThunderstormObject = thorObj;
                    if (showDebug) Debug.Log("WeatherProgressionController: Found THOR Thunderstorm GameObject by name");
                }
                else
                {
                    if (showDebug) Debug.LogWarning("WeatherProgressionController: THOR Thunderstorm not found! Please assign it manually in the Inspector.");
                }
            }
        }

        // Set initial cloud transition speed
        if (weatherModule.Settings != null)
        {
            weatherModule.Settings.cloudsTransitionSpeed = cloudTransitionSpeed;
        }

        // Find WaterRenderer for wind speed manipulation
        waterRenderer = FindObjectOfType<WaterRenderer>();
        if (waterRenderer != null)
        {
            originalWindSpeed = waterRenderer.WindSpeed; // Public property returns km/h
            if (showDebug) Debug.Log($"WeatherProgressionController: Found WaterRenderer with wind speed {originalWindSpeed} km/h");
        }

        // Find Crest Shape Gerstner components
        FindCrestWaveComponents();

        // Initialize wave multiplier
        currentWaveMultiplier = startingWaveMultiplier;
        targetWaveMultiplier = startingWaveMultiplier;
        UpdateWaveMultiplier(startingWaveMultiplier);

        // Start progression if enabled (with optional delay)
        if (startOnStart)
        {
            if (startDelay > 0f)
            {
                StartCoroutine(DelayedStartWeatherProgression());
            }
            else
            {
                StartWeatherProgression();
            }
        }
    }

    /// <summary>
    /// Finds all Shape Gerstner components in the scene
    /// </summary>
    private void FindCrestWaveComponents()
    {
        shapeGerstnerComponents = FindObjectsOfType<ShapeGerstner>();
        
        if (shapeGerstnerComponents == null || shapeGerstnerComponents.Length == 0)
        {
            if (showDebug) Debug.LogWarning("WeatherProgressionController: No Shape Gerstner components found! Waves will not be affected.");
            return;
        }

        if (showDebug) Debug.Log($"WeatherProgressionController: Found {shapeGerstnerComponents.Length} Shape Gerstner component(s)");

        // Get Wave Spectra from Shape Gerstner components
        System.Collections.Generic.List<WaveSpectrum> spectraList = new System.Collections.Generic.List<WaveSpectrum>();
        
        foreach (var shapeGerstner in shapeGerstnerComponents)
        {
            // Use reflection to access the internal _Spectrum field
            var spectrumField = typeof(ShapeWaves).GetField("_Spectrum", BindingFlags.NonPublic | BindingFlags.Instance);
            if (spectrumField != null)
            {
                var spectrum = spectrumField.GetValue(shapeGerstner) as WaveSpectrum;
                if (spectrum != null && !spectraList.Contains(spectrum))
                {
                    spectraList.Add(spectrum);
                }
            }
        }

        waveSpectra = new WaveSpectrum[spectraList.Count];
        for (int i = 0; i < spectraList.Count; i++)
        {
            waveSpectra[i] = spectraList[i];
        }
        
        if (showDebug) Debug.Log($"WeatherProgressionController: Found {waveSpectra.Length} unique Wave Spectrum asset(s)");
    }

    /// <summary>
    /// Updates wave multiplier on all Wave Spectrum assets and forces regeneration
    /// </summary>
    private void UpdateWaveMultiplier(float multiplier)
    {
        if (waveSpectra == null || waveSpectra.Length == 0 || shapeGerstnerComponents == null)
            return;

        // Use reflection to access internal _Multiplier field
        var multiplierField = typeof(WaveSpectrum).GetField("_Multiplier", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (multiplierField == null)
        {
            if (showDebug) Debug.LogWarning("WeatherProgressionController: Could not access WaveSpectrum._Multiplier field!");
            return;
        }

        // Update multiplier on all spectra
        foreach (var spectrum in waveSpectra)
        {
            if (spectrum != null)
            {
                multiplierField.SetValue(spectrum, multiplier);
                
                // Trigger update by calling InitializeHandControls if available
                var initMethod = typeof(WaveSpectrum).GetMethod("InitializeHandControls", BindingFlags.NonPublic | BindingFlags.Instance);
                if (initMethod != null)
                {
                    initMethod.Invoke(spectrum, null);
                }
            }
        }

        // Force Shape Gerstner to regenerate waves
        ForceWaveRegeneration();
    }

    /// <summary>
    /// Forces Shape Gerstner components to regenerate waves by temporarily changing wind speed
    /// </summary>
    private void ForceWaveRegeneration()
    {
        if (waterRenderer == null)
        {
            // Fallback: Reset wind speed tracking via reflection
            if (shapeGerstnerComponents == null || shapeGerstnerComponents.Length == 0)
                return;

            var windSpeedField = typeof(ShapeGerstner).GetField("_WindSpeedWhenGenerated", BindingFlags.NonPublic | BindingFlags.Instance);
            if (windSpeedField != null)
            {
                foreach (var shapeGerstner in shapeGerstnerComponents)
                {
                    if (shapeGerstner != null)
                    {
                        windSpeedField.SetValue(shapeGerstner, -1f);
                    }
                }
            }
            return;
        }

        // Temporarily change wind speed to trigger regeneration
        // This is the most reliable way to force ShapeGerstner to regenerate waves
        StartCoroutine(TemporaryWindSpeedChange());
    }

    /// <summary>
    /// Temporarily changes wind speed to force wave regeneration
    /// Uses smaller change and waits longer to prevent visual glitches
    /// </summary>
    private IEnumerator TemporaryWindSpeedChange()
    {
        if (waterRenderer == null)
            yield break;

        // Store current wind speed override state
        var overrideField = typeof(WaterRenderer).GetField("_OverrideWindZoneWindSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        bool wasOverridden = false;
        if (overrideField != null)
        {
            wasOverridden = (bool)overrideField.GetValue(waterRenderer);
        }

        // Get current wind speed
        float currentWindSpeed = waterRenderer.WindSpeed; // Public property returns km/h

        // Temporarily change wind speed slightly to trigger regeneration
        var windSpeedField = typeof(WaterRenderer).GetField("_WindSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        if (windSpeedField != null)
        {
            // Enable override if not already enabled
            if (overrideField != null && !wasOverridden)
            {
                overrideField.SetValue(waterRenderer, true);
            }

            // Use smaller change (0.05 km/h) to minimize visual glitches
            windSpeedField.SetValue(waterRenderer, currentWindSpeed + 0.05f);
            
            // Wait 2 frames for smoother regeneration (reduces glitches)
            yield return null;
            yield return null;
            
            // Restore original wind speed
            windSpeedField.SetValue(waterRenderer, currentWindSpeed);
            
            // Restore override state
            if (overrideField != null && !wasOverridden)
            {
                overrideField.SetValue(waterRenderer, false);
            }
        }
    }

    void Update()
    {
        // Move transform towards player if assigned and target exists
        if (transformToMove != null && targetTransform != null && isProgressionActive)
        {
            MoveTransformToTarget();
        }

        // Smoothly transition wave multiplier
        if (Mathf.Abs(currentWaveMultiplier - targetWaveMultiplier) > 0.01f)
        {
            // Use smooth interpolation for gradual changes
            currentWaveMultiplier = Mathf.Lerp(currentWaveMultiplier, targetWaveMultiplier, 
                waveIntensityTransitionSpeed * Time.deltaTime);
        }
        
        // Update multiplier value continuously (smoothly)
        // This ensures the multiplier value is always up-to-date without forcing regeneration
        var multiplierField = typeof(WaveSpectrum).GetField("_Multiplier", BindingFlags.NonPublic | BindingFlags.Instance);
        if (multiplierField != null && waveSpectra != null)
        {
            foreach (var spectrum in waveSpectra)
            {
                if (spectrum != null)
                {
                    multiplierField.SetValue(spectrum, currentWaveMultiplier);
                }
            }
        }
        
        // Only force regeneration at intervals to prevent glitches
        // Check if multiplier changed significantly or enough time has passed
        float multiplierChange = Mathf.Abs(currentWaveMultiplier - lastMultiplierValue);
        float timeSinceLastRegen = Time.time - lastRegenerationTime;
        
        // Regenerate if:
        // 1. Multiplier changed significantly (more than 0.05)
        // 2. OR enough time has passed since last regeneration
        if ((multiplierChange > 0.05f || timeSinceLastRegen >= waveRegenerationInterval) && 
            Mathf.Abs(currentWaveMultiplier - lastMultiplierValue) > 0.01f)
        {
            UpdateWaveMultiplier(currentWaveMultiplier);
            lastMultiplierValue = currentWaveMultiplier;
            lastRegenerationTime = Time.time;
        }
    }

    /// <summary>
    /// Verifies that weather types exist in Enviro's settings
    /// </summary>
    private bool VerifyWeatherTypes()
    {
        if (weatherModule == null || weatherModule.Settings == null)
        {
            Debug.LogError("WeatherProgressionController: Weather module or settings not found!");
            return false;
        }

        bool allFound = true;
        string[] weatherNames = { cloudy1WeatherName, cloudy2WeatherName, cloudy3WeatherName, stormWeatherName };
        
        if (showDebug) Debug.Log($"WeatherProgressionController: Verifying weather types...");
        if (showDebug) Debug.Log($"WeatherProgressionController: Available weather types: {weatherModule.Settings.weatherTypes.Count}");
        
        foreach (string weatherName in weatherNames)
        {
            bool found = false;
            foreach (var weatherType in weatherModule.Settings.weatherTypes)
            {
                if (weatherType != null && weatherType.name == weatherName)
                {
                    found = true;
                    if (showDebug) Debug.Log($"WeatherProgressionController: ✓ Found '{weatherName}'");
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogError($"WeatherProgressionController: ✗ Weather type '{weatherName}' NOT FOUND!");
                allFound = false;
                
                // List available weather types for debugging
                if (showDebug)
                {
                    Debug.LogWarning("WeatherProgressionController: Available weather types:");
                    for (int i = 0; i < weatherModule.Settings.weatherTypes.Count; i++)
                    {
                        if (weatherModule.Settings.weatherTypes[i] != null)
                        {
                            Debug.LogWarning($"  [{i}] '{weatherModule.Settings.weatherTypes[i].name}'");
                        }
                    }
                }
            }
        }
        
        return allFound;
    }

    /// <summary>
    /// Coroutine to delay weather progression start
    /// </summary>
    private IEnumerator DelayedStartWeatherProgression()
    {
        if (showDebug) Debug.Log($"WeatherProgressionController: Waiting {startDelay} seconds before starting weather progression...");
        yield return new WaitForSeconds(startDelay);
        
        if (showDebug) Debug.Log("WeatherProgressionController: Starting weather progression after delay");
        StartWeatherProgression();
    }

    /// <summary>
    /// Starts the weather progression sequence
    /// </summary>
    public void StartWeatherProgression()
    {
        if (isProgressionActive)
        {
            if (showDebug) Debug.LogWarning("WeatherProgressionController: Progression already active!");
            return;
        }

        if (weatherModule == null)
        {
            Debug.LogError("WeatherProgressionController: Weather module not initialized!");
            return;
        }

        // Verify weather types exist before starting
        if (!VerifyWeatherTypes())
        {
            Debug.LogError("WeatherProgressionController: Cannot start progression - weather types not found! Check the weather type names in the Inspector.");
            return;
        }

        // Set cloud transition speed for gradual formation BEFORE changing weather
        if (weatherModule.Settings != null)
        {
            weatherModule.Settings.cloudsTransitionSpeed = cloudTransitionSpeed;
            if (showDebug) Debug.Log($"WeatherProgressionController: Cloud transition speed set to {cloudTransitionSpeed}");
        }

        // Set initial weather to Cloudy 1
        if (showDebug) Debug.Log($"WeatherProgressionController: Setting initial weather to {cloudy1WeatherName}");
        ChangeWeatherSafely(cloudy1WeatherName);

        // Verify the change took effect
        if (showDebug && weatherModule.targetWeatherType != null)
        {
            Debug.Log($"WeatherProgressionController: Verified targetWeatherType is now '{weatherModule.targetWeatherType.name}'");
        }

        // Start progression coroutine
        StartCoroutine(WeatherProgressionCoroutine());
    }

    /// <summary>
    /// Changes weather using the same method as WeatherCycleManager (simple and direct)
    /// </summary>
    private bool ChangeWeatherSafely(string weatherName)
    {
        if (enviroManager == null || enviroManager.Weather == null)
        {
            Debug.LogError("WeatherProgressionController: EnviroManager or Weather module not available!");
            return false;
        }

        // Use the same simple approach as WeatherCycleManager - it works!
        enviroManager.Weather.ChangeWeather(weatherName);
        
        if (showDebug) Debug.Log($"WeatherProgressionController: Changed weather to '{weatherName}'");
        return true;
    }

    /// <summary>
    /// Coroutine that handles the weather progression
    /// </summary>
    private IEnumerator WeatherProgressionCoroutine()
    {
        isProgressionActive = true;
        float elapsedTime = 0f;
        float thorActivationTime = totalTransitionDuration * thorActivationPoint;

        if (showDebug) Debug.Log($"WeatherProgressionController: Starting weather progression over {totalTransitionDuration} seconds");

        // Wait a moment to ensure Cloudy 1 is set
        yield return new WaitForSeconds(0.5f);

        // Start gradual wave intensity increase coroutine
        StartCoroutine(GradualWaveIntensityIncrease());

        // Calculate time per weather transition (divide by 3 transitions)
        float timePerTransition = totalTransitionDuration / 3f;

        // Transition 1: Cloudy 1 → Cloudy 2
        yield return new WaitForSeconds(timePerTransition);
        elapsedTime += timePerTransition;
        
        if (showDebug) Debug.Log($"WeatherProgressionController: Transitioning to {cloudy2WeatherName} ({elapsedTime:F1}s elapsed)");
        if (!ChangeWeatherSafely(cloudy2WeatherName))
        {
            Debug.LogError($"WeatherProgressionController: Failed to change to {cloudy2WeatherName}! Stopping progression.");
            yield break;
        }

        // Transition 2: Cloudy 2 → Cloudy 3
        yield return new WaitForSeconds(timePerTransition);
        elapsedTime += timePerTransition;
        
        if (showDebug) Debug.Log($"WeatherProgressionController: Transitioning to {cloudy3WeatherName} ({elapsedTime:F1}s elapsed)");
        if (!ChangeWeatherSafely(cloudy3WeatherName))
        {
            Debug.LogError($"WeatherProgressionController: Failed to change to {cloudy3WeatherName}! Stopping progression.");
            yield break;
        }

        // Activate THOR Thunderstorm just before storm (if not already activated)
        if (!thorActivated && elapsedTime >= thorActivationTime)
        {
            ActivateThorThunderstorm();
        }
        else if (!thorActivated)
        {
            // Wait until activation point
            float waitTime = thorActivationTime - elapsedTime;
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
                elapsedTime = thorActivationTime;
                ActivateThorThunderstorm();
            }
        }

        // Transition 3: Cloudy 3 → Storm
        yield return new WaitForSeconds(timePerTransition);
        elapsedTime += timePerTransition;
        
        if (showDebug) Debug.Log($"WeatherProgressionController: Transitioning to {stormWeatherName} ({elapsedTime:F1}s elapsed)");
        if (!ChangeWeatherSafely(stormWeatherName))
        {
            Debug.LogError($"WeatherProgressionController: Failed to change to {stormWeatherName}! Stopping progression.");
            yield break;
        }

        if (showDebug) Debug.Log("WeatherProgressionController: Weather progression complete!");
    }

    /// <summary>
    /// Gradually increases wave intensity over the entire weather progression duration
    /// </summary>
    private IEnumerator GradualWaveIntensityIncrease()
    {
        float startTime = Time.time;
        float progress = 0f;

        while (progress < 1f && isProgressionActive)
        {
            float timeElapsed = Time.time - startTime;
            progress = Mathf.Clamp01(timeElapsed / totalTransitionDuration);

            // Calculate target multiplier based on progress
            float targetMultiplier;
            
            if (progress < 0.33f) // Cloudy 1 → Cloudy 2 phase
            {
                // Gradually increase from starting to Cloudy 2 multiplier
                float phaseProgress = progress / 0.33f;
                targetMultiplier = Mathf.Lerp(startingWaveMultiplier, cloudy2WaveMultiplier, phaseProgress);
            }
            else if (progress < 0.66f) // Cloudy 2 → Cloudy 3 phase
            {
                // Gradually increase from Cloudy 2 to Cloudy 3 multiplier
                float phaseProgress = (progress - 0.33f) / 0.33f;
                targetMultiplier = Mathf.Lerp(cloudy2WaveMultiplier, cloudy3WaveMultiplier, phaseProgress);
            }
            else // Cloudy 3 → Storm phase
            {
                // Gradually increase from Cloudy 3 to Storm multiplier (with extra intensity)
                float phaseProgress = (progress - 0.66f) / 0.34f;
                // Use smooth curve for dramatic storm ramp-up
                float smoothProgress = Mathf.SmoothStep(0f, 1f, phaseProgress);
                targetMultiplier = Mathf.Lerp(cloudy3WaveMultiplier, maxStormWaveMultiplier, smoothProgress);
            }

            // Update target multiplier (Update() will smoothly interpolate to it)
            targetWaveMultiplier = targetMultiplier;

            yield return null;
        }

        // Ensure we reach max storm multiplier
        targetWaveMultiplier = maxStormWaveMultiplier;
        if (showDebug) Debug.Log($"WeatherProgressionController: Waves reached maximum storm intensity: {maxStormWaveMultiplier}");
    }


    /// <summary>
    /// Activates THOR Thunderstorm system
    /// </summary>
    private void ActivateThorThunderstorm()
    {
        if (thorActivated)
            return;

        thorActivated = true;

        if (thorThunderstormObject == null)
        {
            Debug.LogWarning("WeatherProgressionController: THOR Thunderstorm GameObject not found! Skipping activation.");
            return;
        }

        // Get THOR_Thunderstorm component
        var thorComponent = thorThunderstormObject.GetComponent<THOR_Thunderstorm>();
        if (thorComponent == null)
        {
            Debug.LogWarning("WeatherProgressionController: THOR_Thunderstorm component not found on GameObject!");
            return;
        }

        // Activate THOR Thunderstorm using its API
        if (showDebug) Debug.Log($"WeatherProgressionController: Activating THOR Thunderstorm with intensity {thorIntensity}");
        
        // Use the static API method (namespace is THOR)
        THOR_Thunderstorm.ControlThunderstorm(thorIntensity, thorTransitionDuration);

        // Also enable the GameObject if it's disabled
        if (!thorThunderstormObject.activeSelf)
        {
            thorThunderstormObject.SetActive(true);
        }
    }

    /// <summary>
    /// Moves the transform GameObject towards the target (player/camera)
    /// </summary>
    private void MoveTransformToTarget()
    {
        if (transformToMove == null || targetTransform == null)
            return;

        Vector3 direction = (targetTransform.position - transformToMove.position);
        float distance = direction.magnitude;

        // Stop if close enough
        if (distance <= stopDistance)
            return;

        // Normalize direction and move
        direction.Normalize();
        Vector3 movement = direction * movementSpeed * Time.deltaTime;

        // Move the transform
        transformToMove.position += movement;

        // Optionally rotate to face target
        if (distance > 0.1f)
        {
            transformToMove.LookAt(targetTransform);
        }
    }

    /// <summary>
    /// Stops the weather progression
    /// </summary>
    public void StopWeatherProgression()
    {
        StopAllCoroutines();
        isProgressionActive = false;
        if (showDebug) Debug.Log("WeatherProgressionController: Weather progression stopped");
    }

    /// <summary>
    /// Resets weather progression (can be called to restart)
    /// </summary>
    public void ResetWeatherProgression()
    {
        StopWeatherProgression();
        thorActivated = false;
        
        // Reset wave intensity
        currentWaveMultiplier = startingWaveMultiplier;
        targetWaveMultiplier = startingWaveMultiplier;
        UpdateWaveMultiplier(startingWaveMultiplier);
        
        StartWeatherProgression();
    }

    void OnValidate()
    {
        // Ensure cloud transition speed is set correctly
        if (weatherModule != null && weatherModule.Settings != null)
        {
            weatherModule.Settings.cloudsTransitionSpeed = cloudTransitionSpeed;
        }
    }
}
