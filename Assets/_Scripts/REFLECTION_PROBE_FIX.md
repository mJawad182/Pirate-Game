# Fixing URP Reflection Probe Array Size Error

## Error Message
```
Property (urp_ReflProbes_BoxMin) exceeds previous array size (64 vs 32). 
Cap to previous size. Restart Unity to recreate the arrays.
```

## What This Means

This error occurs when Unity's Universal Render Pipeline (URP) encounters more reflection probes than its internal array can handle. URP has limits on the number of reflection probes it can process simultaneously.

## Quick Fixes

### Method 1: Use the Fix Tool (Recommended)
1. Go to: **Tools > Pirate Escape Room > Fix Reflection Probes (Quick)**
2. The tool will automatically:
   - Remove disabled reflection probes
   - List all probes in the scene
   - Provide recommendations

### Method 2: Manual Fix

#### Step 1: Remove Unnecessary Reflection Probes
1. In Unity Hierarchy, search for "Reflection Probe"
2. Select each probe and check if it's needed
3. Delete probes that are:
   - Disabled
   - Duplicates
   - Not visible in gameplay
   - In unused areas

#### Step 2: Optimize Probe Settings
- Set probes to **Baked** mode instead of **Realtime** when possible
- Use **Box Projection** only when necessary
- Reduce **Resolution** for probes that don't need high quality

#### Step 3: Restart Unity
After removing probes, restart Unity to recreate the internal arrays.

## Understanding Reflection Probe Limits

- **URP typically supports**: 32-64 reflection probes per scene
- **Best practice**: Keep under 32 active probes
- **Real-time probes**: More expensive, use sparingly
- **Baked probes**: Less expensive, use for static scenes

## Prevention

### For Your Pirate Escape Room:

1. **Ocean Scene**: 
   - Use 1-2 reflection probes for the ocean (one for sky, one for water)
   - Set to Baked mode if environment is static

2. **Ship**:
   - Avoid adding reflection probes directly to the ship
   - Let the scene probes handle reflections

3. **Islands**:
   - Use 1 probe per major island area
   - Bake them for better performance

4. **Storm Effects**:
   - Don't add probes for storm clouds
   - Use skybox reflections instead

## Script to Check Probe Count

You can use this in your GameManager or a debug script:

```csharp
void CheckReflectionProbes()
{
    ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>();
    Debug.Log($"Total Reflection Probes: {probes.Length}");
    
    if (probes.Length > 32)
    {
        Debug.LogWarning($"Too many probes! Consider removing {probes.Length - 32} probes.");
    }
}
```

## If Error Persists

1. **Restart Unity** (this often resolves the issue)
2. **Check for hidden probes** in disabled GameObjects
3. **Verify URP version** - older versions may have stricter limits
4. **Consider upgrading URP** if using an older version
5. **Check prefabs** - probes might be in prefabs that aren't visible

## Additional Resources

- Unity URP Documentation: Reflection Probes
- Unity Forums: URP Reflection Probe Limits
- Use the built-in tool: **Tools > Pirate Escape Room > Fix Reflection Probes**

---

**Quick Action**: Run **Tools > Pirate Escape Room > Fix Reflection Probes (Quick)** to automatically fix common issues!



