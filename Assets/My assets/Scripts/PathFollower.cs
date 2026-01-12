using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a path using waypoints that other objects can follow
/// Create empty GameObjects as children to define the path points
/// </summary>
public class PathFollower : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("Waypoints to follow (assign manually or use child objects)")]
    public Transform[] waypoints;
    
    [Tooltip("Use child objects as waypoints automatically")]
    public bool useChildObjectsAsWaypoints = true;
    
    [Tooltip("Loop the path (return to start when reaching end)")]
    public bool loopPath = true;
    
    [Tooltip("Close the path (connect last point to first)")]
    public bool closePath = false;
    
    [Header("Path Visualization")]
    [Tooltip("Show path in editor")]
    public bool showPathInEditor = true;
    
    [Tooltip("Color of path line")]
    public Color pathColor = Color.yellow;
    
    [Header("Character Settings")]
    [Tooltip("Character GameObject to activate (should be inactive initially)")]
    public GameObject characterObject;
    
    [Tooltip("Automatically activate character at first waypoint when game starts")]
    public bool autoActivateOnStart = true;
    
    [Tooltip("Destroy character when it reaches the end of the path (if not looping)")]
    public bool destroyOnPathComplete = true;
    
    private List<Vector3> pathPoints = new List<Vector3>();
    
    void Start()
    {
        BuildPath();
        
        // Activate character at first waypoint if enabled
        if (autoActivateOnStart && characterObject != null)
        {
            ActivateCharacter();
        }
    }
    
    void BuildPath()
    {
        pathPoints.Clear();
        
        if (useChildObjectsAsWaypoints && waypoints == null || waypoints.Length == 0)
        {
            // Get all child transforms as waypoints
            List<Transform> childWaypoints = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                childWaypoints.Add(transform.GetChild(i));
            }
            waypoints = childWaypoints.ToArray();
        }
        
        if (waypoints != null && waypoints.Length > 0)
        {
            foreach (var waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    pathPoints.Add(waypoint.position);
                }
            }
            
            // If close path, add first point at the end
            if (closePath && pathPoints.Count > 0)
            {
                pathPoints.Add(pathPoints[0]);
            }
        }
    }
    
    /// <summary>
    /// Get the path points
    /// </summary>
    public List<Vector3> GetPathPoints()
    {
        if (pathPoints.Count == 0)
        {
            BuildPath();
        }
        return new List<Vector3>(pathPoints);
    }
    
    /// <summary>
    /// Get number of waypoints
    /// </summary>
    public int WaypointCount => pathPoints.Count;
    
    /// <summary>
    /// Get waypoint at index
    /// </summary>
    public Vector3 GetWaypoint(int index)
    {
        if (index >= 0 && index < pathPoints.Count)
        {
            return pathPoints[index];
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Spawn a GameObject at the first waypoint
    /// </summary>
    public void SpawnAtFirstWaypoint(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("PathFollower: Cannot spawn null GameObject at first waypoint!");
            return;
        }
        
        if (pathPoints.Count == 0)
        {
            BuildPath();
        }
        
        if (pathPoints.Count == 0)
        {
            Debug.LogWarning("PathFollower: Cannot spawn at first waypoint - path has no waypoints!");
            return;
        }
        
        // Get first waypoint position
        Vector3 firstWaypoint = pathPoints[0];
        
        // Teleport to first waypoint
        obj.transform.position = firstWaypoint;
        
        // Handle Rigidbody if present
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = firstWaypoint; // Sync Rigidbody position
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Rotate to face next waypoint if available
        if (pathPoints.Count > 1)
        {
            Vector3 nextWaypoint = pathPoints[1];
            Vector3 direction = (nextWaypoint - firstWaypoint).normalized;
            direction.y = 0; // Keep horizontal only
            
            if (direction.magnitude > 0.1f)
            {
                obj.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    /// <summary>
    /// Activate the character at the first waypoint and set it to follow this path
    /// </summary>
    public void ActivateCharacter()
    {
        if (characterObject == null)
        {
            Debug.LogWarning("PathFollower: Cannot activate character - no character GameObject assigned!");
            return;
        }
        
        if (pathPoints.Count == 0)
        {
            BuildPath();
        }
        
        if (pathPoints.Count == 0)
        {
            Debug.LogWarning("PathFollower: Cannot activate character - path has no waypoints!");
            return;
        }
        
        // Activate the character GameObject
        characterObject.SetActive(true);
        
        // Position it at first waypoint
        SpawnAtFirstWaypoint(characterObject);
        
        // Get SwimController and assign this path
        SwimController swimController = characterObject.GetComponent<SwimController>();
        if (swimController != null)
        {
            swimController.SetPath(this);
        }
        else
        {
            Debug.LogWarning("PathFollower: Character does not have SwimController component!");
        }
    }
    
    /// <summary>
    /// Called when character reaches the end of the path
    /// </summary>
    public void OnPathComplete()
    {
        if (destroyOnPathComplete && characterObject != null)
        {
            Destroy(characterObject);
            characterObject = null;
        }
    }
    
    /// <summary>
    /// Spawn a prefab at the first waypoint and return the instance (utility method)
    /// </summary>
    public GameObject SpawnPrefabAtFirstWaypoint(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("PathFollower: Cannot spawn null prefab at first waypoint!");
            return null;
        }
        
        // Instantiate the prefab
        GameObject instance = Instantiate(prefab);
        
        // Spawn it at first waypoint
        SpawnAtFirstWaypoint(instance);
        
        return instance;
    }
    
    void OnDrawGizmos()
    {
        if (!showPathInEditor) return;
        
        BuildPath();
        
        if (pathPoints.Count < 2) return;
        
        Gizmos.color = pathColor;
        
        // Draw lines between waypoints
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
        }
        
        // Draw spheres at waypoints
        Gizmos.color = Color.red;
        foreach (var point in pathPoints)
        {
            Gizmos.DrawSphere(point, 0.5f);
        }
    }
}
