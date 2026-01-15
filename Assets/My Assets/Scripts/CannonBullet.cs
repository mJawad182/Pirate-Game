using UnityEngine;

/// <summary>
/// Controls cannon bullet behavior with projectile motion (curved trajectory)
/// </summary>
public class CannonBullet : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Initial launch speed of the bullet")]
    [Range(10f, 100f)]
    public float launchSpeed = 30f;
    
    [Tooltip("Launch angle in degrees (45 degrees = optimal range)")]
    [Range(0f, 90f)]
    public float launchAngle = 45f;
    
    [Tooltip("Gravity multiplier for projectile motion")]
    [Range(0.5f, 3f)]
    public float gravityMultiplier = 1f;
    
    [Header("Target")]
    [Tooltip("Target ship to aim at (will be set when spawned)")]
    public Transform targetShip;
    
    [Header("Hit Effects")]
    [Tooltip("Particle effect prefab to spawn when bullet hits target (e.g., explosion)")]
    public GameObject hitParticleEffectPrefab;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private Rigidbody rb;
    private Vector3 initialVelocity;
    private bool hasLaunched = false;
    private Vector3 targetPosition;
    private float spawnTime;
    private float collisionIgnoreDuration = 0.5f; // Ignore collisions for first 0.5 seconds
    private GameObject ignoreCollisionWith; // Object to ignore collisions with (firing position)
    
    void Start()
    {
        spawnTime = Time.time;
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure Rigidbody for projectile motion
        rb.useGravity = true;
        rb.mass = 1f;
        rb.linearDamping = 0f; // No air resistance for clean arc
        rb.angularDamping = 0f;
        
        Debug.Log($"[CANNON BULLET] Bullet spawned at {transform.position}, targetShip: {(targetShip != null ? targetShip.name : "NULL")}");
        
        // Calculate and apply initial velocity
        LaunchTowardsTarget();
    }
    
    void FixedUpdate()
    {
        // Update target position if ship is moving
        if (hasLaunched && targetShip != null)
        {
            // Update target position for moving ships
            targetPosition = targetShip.position;
            
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            
            // Check if close enough to target (hit detection) - but only after collision ignore period
            if (Time.time - spawnTime > collisionIgnoreDuration && distanceToTarget < 3f)
            {
                Debug.Log($"[CANNON BULLET] Hit target! Distance: {distanceToTarget:F2}m");
                OnHitTarget();
                return;
            }
            
            // Destroy if too far away (missed) or fallen below water
            if (distanceToTarget > 200f)
            {
                Debug.Log($"[CANNON BULLET] Bullet missed target (too far: {distanceToTarget:F2}m), destroying");
                Destroy(gameObject);
                return;
            }
            
            if (transform.position.y < -10f)
            {
                Debug.Log($"[CANNON BULLET] Bullet fell below water (y: {transform.position.y:F2}), destroying");
                Destroy(gameObject);
                return;
            }
        }
        else if (hasLaunched && targetShip == null)
        {
            // Target was destroyed, destroy bullet
            Debug.LogWarning("[CANNON BULLET] Target ship is null, destroying bullet");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Calculates and applies initial velocity towards target with projectile motion
    /// </summary>
    private void LaunchTowardsTarget()
    {
        if (targetShip == null)
        {
            Debug.LogError("[CANNON BULLET] No target ship assigned! Destroying bullet.");
            Destroy(gameObject);
            return;
        }
        
        targetPosition = targetShip.position;
        Vector3 directionToTarget = (targetPosition - transform.position);
        float horizontalDistance = new Vector3(directionToTarget.x, 0, directionToTarget.z).magnitude;
        float verticalDistance = directionToTarget.y;
        
        // Calculate launch velocity using projectile motion physics
        // v = sqrt(g * d / sin(2θ)) for optimal angle
        // But we'll use a simpler approach: calculate velocity needed for the arc
        
        float gravity = Physics.gravity.magnitude * gravityMultiplier;
        
        // Calculate optimal launch angle based on target distance and height
        float optimalAngle = CalculateOptimalAngle(horizontalDistance, verticalDistance, gravity);
        float angleRad = optimalAngle * Mathf.Deg2Rad;
        
        // Calculate initial velocity components
        float vx = launchSpeed * Mathf.Cos(angleRad);
        float vy = launchSpeed * Mathf.Sin(angleRad);
        
        // Get horizontal direction to target
        Vector3 horizontalDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;
        
        // Set initial velocity
        initialVelocity = horizontalDirection * vx + Vector3.up * vy;
        rb.linearVelocity = initialVelocity;
        
        hasLaunched = true;
        
        if (showDebug)
        {
            Debug.Log($"CannonBullet: Launched towards {targetShip.name} with velocity {initialVelocity}, angle: {optimalAngle:F1}°");
        }
    }
    
    /// <summary>
    /// Calculates optimal launch angle for projectile motion
    /// </summary>
    private float CalculateOptimalAngle(float horizontalDistance, float verticalDistance, float gravity)
    {
        // If using custom launch angle, use it
        if (launchAngle > 0f)
        {
            return launchAngle;
        }
        
        // Calculate optimal angle based on target position
        // For targets at same height: 45 degrees is optimal
        // For targets above: increase angle
        // For targets below: decrease angle
        
        float baseAngle = 45f;
        
        // Adjust angle based on vertical distance
        if (verticalDistance > 0)
        {
            // Target is above, increase angle
            baseAngle += Mathf.Clamp(verticalDistance * 2f, 0f, 30f);
        }
        else
        {
            // Target is below, decrease angle slightly
            baseAngle += Mathf.Clamp(verticalDistance * 1f, -20f, 0f);
        }
        
        return Mathf.Clamp(baseAngle, 15f, 75f);
    }
    
    /// <summary>
    /// Called when bullet hits the target
    /// </summary>
    private void OnHitTarget()
    {
        if (showDebug) Debug.Log($"CannonBullet: Hit target {targetShip.name}!");
        
        // Play hit sound
        CannonAudioHandler.PlayHit();
        
        // Spawn hit particle effect
        SpawnHitParticleEffect(transform.position);
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Spawns the hit particle effect at the hit position
    /// </summary>
    private void SpawnHitParticleEffect(Vector3 hitPosition)
    {
        if (hitParticleEffectPrefab == null)
        {
            if (showDebug) Debug.LogWarning("[CANNON BULLET] Hit particle effect prefab not assigned, skipping particle effect.");
            return;
        }
        
        GameObject particleEffect = Instantiate(hitParticleEffectPrefab, hitPosition, Quaternion.identity);
        
        if (particleEffect != null)
        {
            // Add auto-destroy component to handle cleanup
            ParticleEffectAutoDestroy autoDestroy = particleEffect.GetComponent<ParticleEffectAutoDestroy>();
            if (autoDestroy == null)
            {
                autoDestroy = particleEffect.AddComponent<ParticleEffectAutoDestroy>();
            }
            
            // Try to get ParticleSystem component and play it if it exists
            ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                if (showDebug) Debug.Log($"[CANNON BULLET] Spawned and played hit particle effect at {hitPosition}");
            }
            else
            {
                // Check if there's a ParticleSystem in children
                ps = particleEffect.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    if (showDebug) Debug.Log($"[CANNON BULLET] Spawned and played hit particle effect (from child) at {hitPosition}");
                }
                else
                {
                    if (showDebug) Debug.Log($"[CANNON BULLET] Spawned hit particle effect (no ParticleSystem found) at {hitPosition}");
                }
            }
        }
        else
        {
            Debug.LogError("[CANNON BULLET] Failed to instantiate hit particle effect prefab!");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Ignore collisions for a short time after spawning (to avoid immediate collision with firing position)
        if (Time.time - spawnTime < collisionIgnoreDuration)
        {
            Debug.Log($"[CANNON BULLET] Ignoring collision with {collision.gameObject.name} (too soon after spawn: {Time.time - spawnTime:F2}s)");
            return;
        }
        
        // Ignore collision with the object we're supposed to ignore (firing position)
        if (ignoreCollisionWith != null && collision.gameObject == ignoreCollisionWith)
        {
            Debug.Log($"[CANNON BULLET] Ignoring collision with firing position: {collision.gameObject.name}");
            return;
        }
        
        // Ignore collision with player/cannon objects (you can add more ignore tags here)
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Cannon"))
        {
            Debug.Log($"[CANNON BULLET] Ignoring collision with {collision.gameObject.name} (tag: {collision.gameObject.tag})");
            return;
        }
        
        // Destroy bullet on collision with anything else
        Debug.Log($"[CANNON BULLET] Collided with {collision.gameObject.name}, destroying bullet");
        
        // Play hit sound
        CannonAudioHandler.PlayHit();
        
        // Spawn hit particle effect at collision point
        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        SpawnHitParticleEffect(hitPoint);
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Set an object to ignore collisions with (e.g., firing position)
    /// </summary>
    public void SetIgnoreCollision(GameObject obj)
    {
        ignoreCollisionWith = obj;
    }
}
