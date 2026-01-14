using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns the parrot prefab when hat starts falling
/// Attach this to a controller GameObject (not the parrot itself)
/// </summary>
public class ParrotSpawner : MonoBehaviour
{
    [Header("Parrot Prefab")]
    [Tooltip("The parrot prefab to spawn")]
    public GameObject parrotPrefab;
    
    [Header("Spawn Settings")]
    [Tooltip("Spawn position for the parrot (where it appears)")]
    public Transform spawnPosition;
    
    [Tooltip("Delay after hat starts falling before spawning parrot (seconds)")]
    [Range(0f, 10f)]
    public float spawnDelay = 2f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private HatController hatController;
    private GameObject spawnedParrot;
    
    void Start()
    {
        // Find HatController to subscribe to hat falling event
        hatController = FindObjectOfType<HatController>();
        if (hatController != null)
        {
            // Subscribe to hat falling event
            hatController.OnHatStartedFalling += OnHatStartedFalling;
        }
        else
        {
            Debug.LogWarning("ParrotSpawner: Could not find HatController! Parrot will not spawn automatically.");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from event
        if (hatController != null)
        {
            hatController.OnHatStartedFalling -= OnHatStartedFalling;
        }
    }
    
    /// <summary>
    /// Called when hat starts falling - spawns parrot after delay
    /// </summary>
    private void OnHatStartedFalling()
    {
        if (showDebug) Debug.Log("ParrotSpawner: Hat started falling, spawning parrot in 2 seconds...");
        StartCoroutine(SpawnParrotAfterDelay());
    }
    
    /// <summary>
    /// Spawns the parrot prefab at spawn position after delay
    /// </summary>
    private IEnumerator SpawnParrotAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        
        // Check if parrot prefab is assigned
        if (parrotPrefab == null)
        {
            Debug.LogError("ParrotSpawner: Parrot prefab is not assigned!");
            yield break;
        }
        
        // Determine spawn position
        Vector3 spawnPos = spawnPosition != null ? spawnPosition.position : transform.position;
        Quaternion spawnRot = spawnPosition != null ? spawnPosition.rotation : transform.rotation;
        
        // Instantiate the parrot prefab
        spawnedParrot = Instantiate(parrotPrefab, spawnPos, spawnRot);
        
        // Get ParrotController from spawned parrot and set hat reference if needed
        ParrotController parrotController = spawnedParrot.GetComponent<ParrotController>();
        if (parrotController != null && hatController != null && hatController.separateHatObject != null)
        {
            // Set hat reference if not already set
            if (parrotController.hatObject == null)
            {
                parrotController.hatObject = hatController.separateHatObject;
            }
        }
        
        if (showDebug) Debug.Log("ParrotSpawner: Parrot spawned!");
    }
    
    /// <summary>
    /// Manually spawn parrot (for testing)
    /// </summary>
    public void ManualSpawn()
    {
        if (parrotPrefab == null)
        {
            Debug.LogError("ParrotSpawner: Parrot prefab is not assigned!");
            return;
        }
        
        Vector3 spawnPos = spawnPosition != null ? spawnPosition.position : transform.position;
        Quaternion spawnRot = spawnPosition != null ? spawnPosition.rotation : transform.rotation;
        
        spawnedParrot = Instantiate(parrotPrefab, spawnPos, spawnRot);
        
        ParrotController parrotController = spawnedParrot.GetComponent<ParrotController>();
        if (parrotController != null && hatController != null && hatController.separateHatObject != null)
        {
            if (parrotController.hatObject == null)
            {
                parrotController.hatObject = hatController.separateHatObject;
            }
        }
    }
}
