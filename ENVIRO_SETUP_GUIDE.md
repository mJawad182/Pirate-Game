# Enviro 3 - Sky and Weather Setup Guide

## Quick Start Steps

### Step 1: Add Enviro 3 Prefab to Scene
1. In Unity, navigate to: `Assets/Enviro 3 - Sky and Weather/`
2. Drag the **Enviro 3.prefab** into your scene hierarchy
3. Select the **Enviro 3** object in the hierarchy
4. In the Inspector, find the **Setup** section
5. Assign your **Main Camera** to the **Camera** field

### Step 2: Activate URP Support
1. With **Enviro 3** selected in the hierarchy
2. In the Inspector, find the **Setup** section
3. Click the **"Activate URP Support"** button
4. Wait for Unity to recompile (this may take a moment)
5. You should see: **Render Pipeline: URP** when finished

### Step 3: Enable Depth Texture
1. Go to **Edit > Project Settings**
2. Select **Graphics** in the left panel
3. Double-click your **Render Pipeline Asset** (likely `PC_RPAsset`)
4. In the Render Pipeline Asset inspector, enable **Depth Texture** option

### Step 4: Add Enviro URP Render Feature ⚠️

**If you can see the "Add Renderer Feature" button:**
1. Open your **URP Renderer Data** asset:
   - In Project window, navigate to: `Assets/Settings/`
   - Double-click **PC_Renderer** (or your renderer asset)
2. In the Inspector, scroll down to **Renderer Features** section
3. Click **"Add Renderer Feature"** button (usually at the top of the list)
4. Select **"Enviro URP Render Feature"** from the dropdown menu

**If you CANNOT see the "Add Renderer Feature" button:**
- The render feature has been automatically added to your renderer asset
- Check the **Renderer Features** list - you should see "Enviro URP Render Feature" listed
- If it's not there, the asset file has been manually updated (see below)

### Step 5: Setup Tonemapping (Important!)
Enviro requires tonemapping for best visual results.

**For URP:**
1. Right-click in your scene hierarchy
2. Select **Volume > Global Volume**
3. In the Global Volume component:
   - Create a new Profile (or assign existing)
   - Click **Add Override** button
   - Select **Tonemapping**
   - Set mode to **ACES** for best results
4. On your **Main Camera**, enable **Post Processing** option

### Step 6: Deactivate Old Directional Light
1. Find your old directional light in the scene (usually named "Directional Light")
2. Deactivate it (Enviro will control lighting)

### Step 7: Test Your Scene
1. Press **Play** to test
2. You should see the Enviro sky and weather system working

---

## Troubleshooting

### Issue: Can't find "Add Renderer Feature" button
**Solution:** The button is located in the **Renderer Features** section of your URP Renderer Data asset inspector. If you still can't see it:
1. Make sure you've selected the Renderer Data asset (not the Pipeline Asset)
2. The asset should be at: `Assets/Settings/PC_Renderer.asset`
3. The render feature may have already been added automatically

### Issue: Enviro effects not rendering
- Check that URP Support is activated (Step 2)
- Verify the render feature is added (Step 4)
- Ensure Depth Texture is enabled (Step 3)
- Make sure Post Processing is enabled on your camera

### Issue: Scene looks too bright/dark
- Add tonemapping (Step 5) - this is required for proper visuals
- Adjust Enviro's sky intensity in the Sky Module settings

---

## Additional Configuration

### Modules
Enviro 3 uses a modular system. You can add/remove modules in the Enviro Manager:
- **Time Module**: Controls date/time and location
- **Lighting Module**: Controls sun/moon lighting
- **Sky Module**: Controls sky appearance
- **Fog Module**: Controls fog and volumetrics
- **Volumetric Clouds Module**: Advanced cloud rendering
- **Weather Module**: Weather system with transitions
- And more...

### Weather System
1. In Enviro Manager, find the **Weather Module**
2. Click **"Add"** to add existing weather types
3. Click **"Create New"** to create custom weather
4. Use **"Set Active"** to change weather types
5. Weather transitions are smooth and automatic

---

## API Examples

### Change Weather
```csharp
Enviro.EnviroManager.instance.Weather.ChangeWeather("Rain");
```

### Set Time of Day
```csharp
Enviro.EnviroManager.instance.Time.SetTimeOfDay(12.5f); // Noon
```

### Get Current Weather
```csharp
Enviro.EnviroWeatherType currentWeather = 
    Enviro.EnviroManager.instance.Weather.targetWeatherType;
```

---

## Next Steps
1. Configure your desired modules in Enviro Manager
2. Set up weather types for your game
3. Adjust sky colors and lighting to match your art style
4. Add audio effects for weather (thunder, rain, etc.)
5. Test different times of day and weather conditions

For more information, refer to the Enviro 3 documentation PDF.

---

## Fixing Rain Coverage - Making Rain Appear Everywhere

If rain only appears around the camera, the particle system's spawn area is too small. Here's how to fix it:

### Method 1: Increase Rain Area in Unity Editor (Recommended)

1. **Find the Rain Particle System:**
   - In your scene hierarchy, expand the **Enviro 3** object
   - Expand **Effects** child object
   - Find **"Rain Particle System"** (or similar name)

2. **Modify the Shape Module:**
   - Select the Rain Particle System GameObject
   - In the Inspector, find the **Particle System** component
   - Expand the **Shape** module (check the box to enable if disabled)
   - Change the **Shape** type to **"Box"** (if not already)
   - Increase the **Box X** and **Box Z** values (these control width and depth)
     - Try values like **50, 50, 50** for a 50x50 unit area
     - Or **100, 100, 100** for a 100x100 unit area
     - Adjust based on your scene size
   - The **Box Y** controls height - keep this high (like 25-50) so rain spawns above

3. **Alternative: Use Sphere Shape:**
   - Change Shape type to **"Sphere"**
   - Increase the **Radius** value (try 50-100 or more)
   - This creates a spherical rain area around the Enviro object

4. **Adjust Emission Rate (Optional):**
   - In the **Emission** module, increase **Rate over Time** if needed
   - This controls how many rain particles spawn per second

### Method 2: Modify the Rain Prefab (Permanent Fix)

1. **Open the Rain Prefab:**
   - Navigate to: `Assets/Enviro 3 - Sky and Weather/Prefabs/Particle Systems/`
   - Double-click **Rain.prefab** to open it in Prefab Mode

2. **Modify Shape Settings:**
   - Select the root GameObject of the prefab
   - In Particle System component → **Shape** module:
     - Set **Shape** to **"Box"**
     - Set **Box X** = **100** (or your desired width)
     - Set **Box Y** = **50** (height - spawn area above ground)
     - Set **Box Z** = **100** (or your desired depth)

3. **Save the Prefab:**
   - Click **"Open Prefab"** button (top bar) to exit Prefab Mode
   - Or press **Ctrl+S** (Windows) / **Cmd+S** (Mac) to save

4. **Apply Changes:**
   - Go back to your Enviro Manager
   - In **Effects Module**, click **"Apply Changes"** button
   - This will recreate the rain system with new settings

### Method 3: Use Multiple Rain Systems (For Very Large Areas)

If you need rain over an extremely large area:

1. **Create Additional Rain Effects:**
   - In Enviro Manager → **Effects Module**
   - Click **"Add"** to add another rain effect
   - Name it "Rain 2" or similar
   - Assign the same Rain prefab
   - Set different **Local Position Offset** values to spread them out
     - Example: Rain 1 at (0, 25, 0), Rain 2 at (100, 25, 0), Rain 3 at (-100, 25, 0)
   - Click **"Apply Changes"**

### Quick Settings Reference

**For Small Scenes (indoor/close-up):**
- Shape: Box
- Box X: 20, Box Y: 25, Box Z: 20

**For Medium Scenes (typical game level):**
- Shape: Box  
- Box X: 100, Box Y: 50, Box Z: 100

**For Large Scenes (open world):**
- Shape: Box
- Box X: 500, Box Y: 100, Box Z: 500
- Or use multiple rain systems positioned around the scene

**For Maximum Coverage (everywhere):**
- Shape: Sphere
- Radius: 1000+ (very large radius)
- Note: This may impact performance with many particles

### Performance Tips

- Larger rain areas = more particles = lower performance
- Consider using **LOD (Level of Detail)** or reducing **Max Particles** in the particle system
- Use **Simulation Space: World** if you want rain to stay in world positions (not follow camera)
- Adjust **Start Lifetime** to ensure particles cover the full area before disappearing

### Troubleshooting

**Rain still only around camera:**
- Make sure you modified the **Shape** module, not just emission rate
- Check that the particle system is using **Local** or **World** simulation space appropriately
- Verify the Enviro object's **Optional Follow Transform** is set correctly (see earlier section)

**Rain disappears when moving:**
- Increase the **Start Lifetime** in the particle system
- Or change **Simulation Space** to **World** instead of **Local**

**Performance issues:**
- Reduce **Max Particles** in the particle system
- Lower the **Rate over Time** in Emission module
- Use multiple smaller systems instead of one huge system
