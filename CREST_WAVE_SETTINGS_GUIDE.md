# Crest Water - How to Make Waves Bigger

## Main Settings Location

### 1. **Wave Spectrum Asset** (Most Important!)
This controls the overall wave amplitude and size.

**Where to Find:**
The Wave Spectrum is on a **Shape component** (like ShapeGerstner or ShapeFFT), NOT directly on AnimatedWavesLod!

**Method 1: Check WaterRenderer GameObject**
1. Select your **WaterRenderer** GameObject in the scene
2. Look in the Inspector for components named:
   - **"Shape Gerstner"** OR
   - **"Shape FFT"** OR
   - **"Shape Waves"**
3. Expand that component
4. Find the **"Waves"** section
5. Look for **"Spectrum"** field (it's a ScriptableObject asset)
6. Click the circle icon next to it to select the asset

**Method 2: Check Child Objects**
1. Select your **WaterRenderer** GameObject
2. Expand it in the Hierarchy to see child objects
3. Look for objects like:
   - "Managed" → might contain Shape components
   - "WaterSwellWaves" or similar
4. Select those child objects and check for **Shape Gerstner** or **Shape FFT** components
5. The Wave Spectrum will be in those components

**Method 3: Search for Shape Components**
1. In Unity, go to **Edit** → **Find References In Scene**
2. Or use **Hierarchy** search and type "Shape"
3. Look for GameObjects with Shape components attached

**Settings to Change:**
- **Multiplier** - This is the MAIN setting! 
  - Range: 0 to 10
  - **Default: 1**
  - **To make waves bigger: Increase to 2, 3, 4, or higher**
  - This scales ALL waves uniformly

- **Gravity Scale** - Affects wave speed (not size directly)
  - Range: 0 to 25
  - Higher = faster waves

- **Power Logarithmic Scales** - Advanced: Controls individual wave octaves
  - Each octave represents different wave sizes (small ripples to large swells)
  - You can edit these in the Wave Spectrum editor window

### 2. **Animated Waves Settings** (On WaterRenderer)

**Where to Find:**
1. Select **WaterRenderer** GameObject
2. In Inspector, expand **"Animated Waves"** section

**Settings:**
- **Wave Spectrum** - Assign/select your Wave Spectrum asset here
- **Maximum Vertical Displacement** - Limits how high waves can go
  - Increase this if waves are being clamped/cut off
- **Resolution** - Higher = more detail (doesn't make waves bigger, just more detailed)

### 3. **Dynamic Waves Settings** (For Interactive Waves)

**Where to Find:**
1. Select **WaterRenderer** GameObject
2. In Inspector, expand **"Dynamic Waves"** section
3. Click on the **Settings** asset

**Settings:**
- **Gravity Multiplier** - Affects dynamic wave speed
- **Horizontal Displace** - Sharpens waves (0-20)
- **Displace Clamp** - Prevents self-intersection (0-1)

## Quick Steps to Make Waves Bigger:

### Method 1: Increase Multiplier (Easiest)
1. Select **WaterRenderer** GameObject in scene
2. Look for **"Shape Gerstner"** or **"Shape FFT"** component in Inspector
   - If not found, check child objects (expand WaterRenderer in Hierarchy)
3. Expand the Shape component → **"Waves"** section
4. Find **"Spectrum"** field and select the asset
5. Increase **"Multiplier"** from 1 to 2, 3, or 4
6. **Done!** Waves should be bigger now

**If you don't see a Shape component:**
- You may need to add one: **Component** → **Crest** → **Inputs** → **Shape Gerstner**
- Or check if it's on a child GameObject

### Method 2: Edit Wave Spectrum Power Values
1. Find the **Shape component** (Shape Gerstner or Shape FFT) on WaterRenderer or its children
2. Expand **"Waves"** section → **"Spectrum"** field
3. Select the Wave Spectrum asset
4. Click the **"Edit"** button (or double-click the asset)
5. In the Wave Spectrum editor window, you'll see a graph
6. **Drag the power values UP** for the wave octaves you want bigger
7. Higher values = bigger waves for that wavelength range

### Method 3: Create New Wave Spectrum
1. Right-click in Project window → **Create** → **Crest** → **Wave Spectrum**
2. Name it "BigWaves" or similar
3. Set **Multiplier** to 2-4
4. Find your **Shape component** (Shape Gerstner or Shape FFT)
5. Assign the new Wave Spectrum to the **"Spectrum"** field in the **"Waves"** section

## Important Notes:

⚠️ **Maximum Vertical Displacement:**
- If waves seem cut off, increase **"Maximum Vertical Displacement"** in Animated Waves settings
- This prevents waves from being clamped

⚠️ **Performance:**
- Bigger waves = more GPU work
- Very high multipliers (5+) may cause performance issues

⚠️ **Visual Quality:**
- After increasing wave size, you might want to:
  - Increase **Resolution** for more detail
  - Adjust **Maximum Vertical Displacement** if waves are clipped
  - Increase **LOD Levels** for better distance rendering

## Recommended Settings for Big Waves:

```
Wave Spectrum Multiplier: 2.5 - 4.0
Maximum Vertical Displacement: 10 - 20
Resolution: 256 - 512 (depending on performance)
```

## Troubleshooting:

**Can't find Wave Spectrum?**
- **Look for Shape components** - Wave Spectrum is NOT on AnimatedWavesLod directly!
- Check WaterRenderer GameObject for "Shape Gerstner" or "Shape FFT" components
- Check child objects (especially "Managed" folder)
- If no Shape component exists, add one: **Component** → **Crest** → **Inputs** → **Shape Gerstner**

**Waves not getting bigger?**
- Make sure you're editing the correct Wave Spectrum asset
- Check that Maximum Vertical Displacement isn't too low (in Animated Waves settings)
- Verify the Wave Spectrum is assigned to the Shape component's Spectrum field
- Make sure the Shape component is enabled

**Waves look weird/glitchy?**
- Reduce Multiplier slightly
- Increase Resolution
- Check Dynamic Waves settings aren't conflicting

**Performance issues?**
- Reduce Resolution
- Lower LOD Levels
- Reduce Multiplier
