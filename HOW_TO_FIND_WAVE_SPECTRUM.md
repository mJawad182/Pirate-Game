# How to Find Wave Spectrum in Crest - Step by Step

## ⚠️ Important: Wave Spectrum is NOT in WaterRenderer.cs!

The Wave Spectrum setting is **NOT** in the WaterRenderer script itself. It's on a **separate component** that needs to be added to your scene.

## Step-by-Step Instructions:

### Step 1: Find Your WaterRenderer GameObject
1. In Unity Hierarchy, find your **WaterRenderer** GameObject
   - It might be named "Water", "Ocean", "CrestWater", or similar
   - Or look for the GameObject that has the **WaterRenderer** component

### Step 2: Check the WaterRenderer GameObject Itself
1. Select the **WaterRenderer** GameObject
2. Look in the **Inspector** panel
3. Scroll through ALL components on that GameObject
4. Look for one of these components:
   - **"Shape Gerstner"** ← This is what you need!
   - **"Shape FFT"** ← Or this one!
   - **"Shape Waves"** ← Or this!

### Step 3: If Not Found, Check Child Objects
1. In the **Hierarchy**, expand the **WaterRenderer** GameObject (click the arrow)
2. Look for child objects, especially:
   - **"Managed"** folder/object
   - **"Container"** object
   - Any object with "Shape" or "Wave" in the name
3. Select each child object and check its Inspector for Shape components

### Step 4: If Still Not Found - Add the Component!
If you don't have a Shape component, you need to add one:

1. Select your **WaterRenderer** GameObject
2. Click **"Add Component"** button (at bottom of Inspector)
3. Search for: **"Shape Gerstner"**
4. Or go to: **Crest** → **Inputs** → **Shape Gerstner**
5. Add the component

### Step 5: Find Wave Spectrum in the Shape Component
Once you find the **Shape Gerstner** (or Shape FFT) component:

1. In the Inspector, expand the **Shape Gerstner** component
2. Look for the **"Waves"** section (it has a heading)
3. Inside "Waves", find the **"Spectrum"** field
4. This is a ScriptableObject asset reference
5. Click the **circle icon** next to it to select the Wave Spectrum asset

### Step 6: Edit the Wave Spectrum
1. After clicking the circle icon, select the Wave Spectrum asset
2. In the Inspector, you'll see the Wave Spectrum settings
3. Find **"Multiplier"** field
4. Change it from **1** to **2, 3, or 4** to make waves bigger!

## Visual Guide:

```
Unity Hierarchy:
└─ WaterRenderer GameObject (or "Water", "Ocean", etc.)
   ├─ Inspector shows:
   │   ├─ WaterRenderer component ← You're looking here
   │   │   └─ Simulations section
   │   │       └─ Animated Waves ← NOT HERE!
   │   │
   │   └─ Shape Gerstner component ← LOOK HERE INSTEAD!
   │       └─ Waves section
   │           └─ Spectrum [Asset] ← THIS IS IT!
   │               └─ Multiplier: 1 → Change to 2-4
   │
   └─ Child Objects (expand to see):
       └─ Managed (or Container)
           └─ Shape Gerstner ← OR CHECK HERE!
```

## Quick Checklist:

- [ ] Found WaterRenderer GameObject in scene
- [ ] Checked Inspector for "Shape Gerstner" or "Shape FFT" component
- [ ] Expanded child objects in Hierarchy
- [ ] Found "Spectrum" field in Shape component's "Waves" section
- [ ] Selected Wave Spectrum asset
- [ ] Changed "Multiplier" from 1 to 2-4

## If You Still Can't Find It:

**Option 1: Add Shape Component Manually**
1. Select WaterRenderer GameObject
2. **Add Component** → Search "Shape Gerstner"
3. The component will appear with a Spectrum field
4. Assign a Wave Spectrum asset to it

**Option 2: Check Project Assets**
1. In Project window, search for "Wave Spectrum"
2. You might find Wave Spectrum assets in:
   - `Packages/com.waveharmonic.crest/Runtime/Data/WaveSpectra/`
   - Or in your Assets folder
3. Double-click one to edit it
4. Change the Multiplier there

## Summary:

**Wave Spectrum is NOT in WaterRenderer.cs!**
- It's on a **Shape component** (ShapeGerstner or ShapeFFT)
- This component is on the **WaterRenderer GameObject** or its **children**
- The setting is in: **Shape Component** → **Waves Section** → **Spectrum Field** → **Multiplier**
