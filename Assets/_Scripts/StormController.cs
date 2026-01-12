using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the approaching storm system for the pirate escape room.
/// Manages storm progression, visual effects, and environmental changes.
/// </summary>
public class StormController : MonoBehaviour
{
    [Header("Storm Settings")]
    [Tooltip("Starting distance of storm from ship")]
    public float initialStormDistance = 1000f;
    
    [Tooltip("Speed at which storm approaches (units per second)")]
    public float stormApproachSpeed = 10f;
    
    [Tooltip("Minimum distance before storm reaches ship")]
    public float minStormDistance = 50f;
    
    [Tooltip("Enable/disable storm movement")]
    public bool stormActive = true;
    
    [Header("Storm Visual Effects")]
    [Tooltip("Storm cloud/prefab to move")]
    public GameObject stormCloudPrefab;
    
    [Tooltip("Particle system for rain")]
    public ParticleSystem rainParticleSystem;
    
    [Tooltip("Particle system for lightning")]
    public ParticleSystem lightningParticleSystem;
    
    [Tooltip("Light for lightning flashes")]
    public Light lightningLight;
    
    [Header("Ocean/Wave Effects")]
    [Tooltip("Reference to Crest water system or wave controller")]
    public MonoBehaviour waveController;
    
    [Tooltip("Base wave amplitude")]
    public float baseWaveAmplitude = 1f;
    
    [Tooltip("Maximum wave amplitude when storm is close")]
    public float maxWaveAmplitude = 5f;
    
    [Header("Audio")]
    [Tooltip("Thunder audio source")]
    public AudioSource thunderAudioSource;
    
    [Tooltip("Wind audio source")]
    public AudioSource windAudioSource;
    
    [Tooltip("Thunder clips")]
    public AudioClip[] thunderClips;
    
    [Header("Storm Progression")]
    [Tooltip("Time between lightning strikes (seconds)")]
    public float lightningIntervalMin = 2f;
    public float lightningIntervalMax = 8f;
    
    [Tooltip("Duration of lightning flash (seconds)")]
    public float lightningDuration = 0.2f;
    
    [Header("Debug")]
    public bool showDebug = true;
    
    private float currentStormDistance;
    private Transform shipTransform;
    private Transform stormTransform;
    private Coroutine lightningCoroutine;
    private Coroutine thunderCoroutine;
    private bool stormReached = false;
    
    // Storm intensity (0 = far away, 1 = very close)
    public float StormIntensity { get; private set; }
    
    void Start()
    {
        currentStormDistance = initialStormDistance;
        
        // Find ship transform
        GameObject ship = GameObject.FindGameObjectWithTag("Player");
        if (ship == null)
        {
            ship = GameObject.Find("Ship");
        }
        if (ship != null)
        {
            shipTransform = ship.transform;
        }
        
        // Create storm cloud if prefab is assigned
        if (stormCloudPrefab != null)
        {
            GameObject stormObj = Instantiate(stormCloudPrefab);
            stormTransform = stormObj.transform;
            UpdateStormPosition();
        }
        else
        {
            // Create a simple storm object at the starting position
            GameObject stormObj = new GameObject("Storm");
            stormTransform = stormObj.transform;
            UpdateStormPosition();
        }
        
        // Initialize audio
        if (windAudioSource != null)
        {
            windAudioSource.loop = true;
            windAudioSource.Play();
        }
        
        // Start lightning effects
        if (lightningCoroutine == null)
        {
            lightningCoroutine = StartCoroutine(LightningRoutine());
        }
        
        if (thunderCoroutine == null && thunderAudioSource != null)
        {
            thunderCoroutine = StartCoroutine(ThunderRoutine());
        }
    }
    
    void Update()
    {
        if (stormActive && !stormReached)
        {
            // Move storm closer
            currentStormDistance -= stormApproachSpeed * Time.deltaTime;
            currentStormDistance = Mathf.Max(currentStormDistance, minStormDistance);
            
            // Update storm position
            UpdateStormPosition();
            
            // Calculate storm intensity
            float distanceRange = initialStormDistance - minStormDistance;
            StormIntensity = 1f - ((currentStormDistance - minStormDistance) / distanceRange);
            StormIntensity = Mathf.Clamp01(StormIntensity);
            
            // Update effects based on intensity
            UpdateStormEffects();
            
            // Check if storm reached
            if (currentStormDistance <= minStormDistance)
            {
                OnStormReached();
            }
            
            if (showDebug)
            {
                Debug.Log($"Storm Distance: {currentStormDistance:F1}m, Intensity: {StormIntensity:F2}");
            }
        }
    }
    
    /// <summary>
    /// Update storm cloud position based on current distance
    /// </summary>
    void UpdateStormPosition()
    {
        if (stormTransform == null || shipTransform == null) return;
        
        // Position storm in front of ship (or adjust direction as needed)
        Vector3 direction = Vector3.forward; // Adjust this based on your scene setup
        stormTransform.position = shipTransform.position + direction * currentStormDistance;
        stormTransform.position = new Vector3(stormTransform.position.x, 
            shipTransform.position.y + 20f, // Storm clouds above
            stormTransform.position.z);
    }
    
    /// <summary>
    /// Update visual and audio effects based on storm intensity
    /// </summary>
    void UpdateStormEffects()
    {
        // Update rain intensity
        if (rainParticleSystem != null)
        {
            var emission = rainParticleSystem.emission;
            emission.rateOverTime = StormIntensity * 1000f;
            
            var main = rainParticleSystem.main;
            main.startSpeed = 5f + (StormIntensity * 10f);
        }
        
        // Update wind audio volume
        if (windAudioSource != null)
        {
            windAudioSource.volume = 0.3f + (StormIntensity * 0.7f);
            windAudioSource.pitch = 0.8f + (StormIntensity * 0.4f);
        }
        
        // Update wave amplitude (if Crest or wave system is available)
        UpdateWaveAmplitude();
    }
    
    /// <summary>
    /// Update ocean wave amplitude based on storm intensity
    /// </summary>
    void UpdateWaveAmplitude()
    {
        if (waveController == null) return;
        
        // Try to update Crest ocean settings via reflection
        // This is a generic approach - adjust based on your wave system
        try
        {
            var amplitudeProperty = waveController.GetType().GetProperty("WaveAmplitude");
            if (amplitudeProperty != null)
            {
                float amplitude = Mathf.Lerp(baseWaveAmplitude, maxWaveAmplitude, StormIntensity);
                amplitudeProperty.SetValue(waveController, amplitude);
            }
        }
        catch (System.Exception e)
        {
            if (showDebug)
            {
                Debug.LogWarning($"Could not update wave amplitude: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Lightning flash routine
    /// </summary>
    IEnumerator LightningRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(lightningIntervalMin, lightningIntervalMax);
            // Lightning happens more frequently as storm approaches
            waitTime *= (2f - StormIntensity);
            
            yield return new WaitForSeconds(waitTime);
            
            // Flash lightning
            if (lightningLight != null)
            {
                lightningLight.enabled = true;
                yield return new WaitForSeconds(lightningDuration);
                lightningLight.enabled = false;
            }
            
            // Lightning particle effect
            if (lightningParticleSystem != null)
            {
                lightningParticleSystem.Play();
            }
        }
    }
    
    /// <summary>
    /// Thunder sound routine (plays after lightning)
    /// </summary>
    IEnumerator ThunderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(lightningIntervalMin + 0.5f, lightningIntervalMax + 2f));
            
            if (thunderAudioSource != null && thunderClips != null && thunderClips.Length > 0)
            {
                AudioClip clip = thunderClips[Random.Range(0, thunderClips.Length)];
                thunderAudioSource.PlayOneShot(clip, 0.5f + (StormIntensity * 0.5f));
            }
        }
    }
    
    /// <summary>
    /// Called when storm reaches minimum distance
    /// </summary>
    void OnStormReached()
    {
        stormReached = true;
        Debug.LogWarning("STORM HAS REACHED THE SHIP!");
        
        // Trigger game event or puzzle requirement
        OnStormArrived?.Invoke();
    }
    
    /// <summary>
    /// Event triggered when storm arrives
    /// </summary>
    public System.Action OnStormArrived;
    
    /// <summary>
    /// Set storm distance manually (useful for testing or puzzle triggers)
    /// </summary>
    public void SetStormDistance(float distance)
    {
        currentStormDistance = Mathf.Clamp(distance, minStormDistance, initialStormDistance);
        UpdateStormPosition();
    }
    
    /// <summary>
    /// Pause/resume storm movement
    /// </summary>
    public void SetStormActive(bool active)
    {
        stormActive = active;
    }
    
    /// <summary>
    /// Get current storm distance
    /// </summary>
    public float GetStormDistance()
    {
        return currentStormDistance;
    }
    
    void OnDrawGizmosSelected()
    {
        if (shipTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shipTransform.position + Vector3.forward * currentStormDistance, 50f);
        }
    }
}



