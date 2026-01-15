using UnityEngine;

/// <summary>
/// Handles audio playback for cannon firing and hit sounds
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CannonAudioHandler : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Audio clip to play when cannon fires")]
    public AudioClip cannonFireClip;
    
    [Tooltip("Audio clip to play when cannon hits target")]
    public AudioClip cannonHitClip;
    
    [Header("Audio Settings")]
    [Tooltip("Volume for firing sound")]
    [Range(0f, 1f)]
    public float fireVolume = 1f;
    
    [Tooltip("Volume for hit sound")]
    [Range(0f, 1f)]
    public float hitVolume = 1f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private AudioSource audioSource;
    private static CannonAudioHandler instance;
    
    void Awake()
    {
        // Set up singleton pattern
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("[CannonAudioHandler] Multiple instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound by default (can be changed in Inspector)
    }
    
    /// <summary>
    /// Plays the cannon firing sound
    /// </summary>
    public void PlayFireSound()
    {
        if (cannonFireClip == null)
        {
            if (showDebug) Debug.LogWarning("[CannonAudioHandler] Cannon fire clip not assigned!");
            return;
        }
        
        if (audioSource != null)
        {
            audioSource.PlayOneShot(cannonFireClip, fireVolume);
            if (showDebug) Debug.Log($"[CannonAudioHandler] Playing fire sound: {cannonFireClip.name}");
        }
    }
    
    /// <summary>
    /// Plays the cannon hit sound
    /// </summary>
    public void PlayHitSound()
    {
        if (cannonHitClip == null)
        {
            if (showDebug) Debug.LogWarning("[CannonAudioHandler] Cannon hit clip not assigned!");
            return;
        }
        
        if (audioSource != null)
        {
            audioSource.PlayOneShot(cannonHitClip, hitVolume);
            if (showDebug) Debug.Log($"[CannonAudioHandler] Playing hit sound: {cannonHitClip.name}");
        }
    }
    
    /// <summary>
    /// Static method to play fire sound (uses singleton instance)
    /// </summary>
    public static void PlayFire()
    {
        if (instance != null)
        {
            instance.PlayFireSound();
        }
        else
        {
            Debug.LogWarning("[CannonAudioHandler] No instance found! Make sure CannonAudioHandler is in the scene.");
        }
    }
    
    /// <summary>
    /// Static method to play hit sound (uses singleton instance)
    /// </summary>
    public static void PlayHit()
    {
        if (instance != null)
        {
            instance.PlayHitSound();
        }
        else
        {
            Debug.LogWarning("[CannonAudioHandler] No instance found! Make sure CannonAudioHandler is in the scene.");
        }
    }
}
