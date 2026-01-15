using UnityEngine;
using System.Collections;

/// <summary>
/// Automatically destroys a particle effect GameObject after its ParticleSystem finishes playing once.
/// Attach this script directly to your particle effect prefab.
/// </summary>
public class ParticleEffectAutoDestroy : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Check all ParticleSystems in children recursively")]
    public bool checkAllChildren = true;
    
    [Tooltip("Additional delay after particles finish (in seconds)")]
    [Range(0f, 5f)]
    public float additionalDelay = 0f;
    
    [Tooltip("Maximum lifetime if no ParticleSystem found (in seconds)")]
    [Range(1f, 30f)]
    public float maxLifetimeFallback = 5f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private ParticleSystem[] particleSystems;
    private bool hasStarted = false;
    
    void Start()
    {
        // Find all ParticleSystem components
        if (checkAllChildren)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
        }
        else
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                particleSystems = new ParticleSystem[] { ps };
            }
            else
            {
                ps = GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    particleSystems = new ParticleSystem[] { ps };
                }
            }
        }
        
        if (particleSystems != null && particleSystems.Length > 0)
        {
            if (showDebug) Debug.Log($"[ParticleEffectAutoDestroy] Found {particleSystems.Length} ParticleSystem(s) on {gameObject.name}");
            
            // Start checking if particle systems are done
            StartCoroutine(CheckAndDestroy());
        }
        else
        {
            // If no ParticleSystem found, destroy after fallback duration
            if (showDebug) Debug.LogWarning($"[ParticleEffectAutoDestroy] No ParticleSystem found on {gameObject.name}, destroying after {maxLifetimeFallback} seconds");
            Destroy(gameObject, maxLifetimeFallback);
        }
    }
    
    private IEnumerator CheckAndDestroy()
    {
        // Wait until at least one particle system starts playing
        bool anyPlaying = false;
        while (!anyPlaying)
        {
            anyPlaying = false;
            foreach (var ps in particleSystems)
            {
                if (ps != null && ps.isPlaying)
                {
                    anyPlaying = true;
                    break;
                }
            }
            
            if (!anyPlaying)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        hasStarted = true;
        if (showDebug) Debug.Log($"[ParticleEffectAutoDestroy] Particle effect started playing on {gameObject.name}");
        
        // Wait until all particle systems finish and all particles are gone
        bool allFinished = false;
        while (!allFinished)
        {
            allFinished = true;
            
            foreach (var ps in particleSystems)
            {
                if (ps != null && (ps.isPlaying || ps.particleCount > 0))
                {
                    allFinished = false;
                    break;
                }
            }
            
            if (!allFinished)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Calculate maximum particle lifetime for safety buffer
        float maxLifetime = 0f;
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var main = ps.main;
                float lifetime = main.startLifetime.constantMax;
                if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                {
                    lifetime = main.startLifetime.constantMax;
                }
                else if (main.startLifetime.mode == ParticleSystemCurveMode.Curve)
                {
                    lifetime = main.startLifetime.curveMultiplier;
                }
                
                if (lifetime > maxLifetime)
                {
                    maxLifetime = lifetime;
                }
            }
        }
        
        // Wait for additional buffer time
        if (maxLifetime > 0)
        {
            yield return new WaitForSeconds(maxLifetime);
        }
        
        // Add any additional delay
        if (additionalDelay > 0)
        {
            yield return new WaitForSeconds(additionalDelay);
        }
        
        // Destroy the particle effect
        if (showDebug) Debug.Log($"[ParticleEffectAutoDestroy] Destroying particle effect: {gameObject.name}");
        Destroy(gameObject);
    }
}
