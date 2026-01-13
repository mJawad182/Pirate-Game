# Crest LOD Settings Guide

This guide explains where to find and adjust LOD (Level of Detail) settings for Crest Ocean System in Unity.

## Method 1: Using the CrestLodSettingsController Script (Recommended)

### Setup:
1. **Add the Script**: 
   - Create an empty GameObject in your scene (or use an existing one)
   - Add the `CrestLodSettingsController` component to it

2. **Find in Inspector**:
   - Select the GameObject with the script
   - In the Inspector, you'll see the **Crest Lod Settings Controller** component

3. **Adjust Settings**:
   - **LOD Levels** (1-15): Controls how many detail levels are rendered
     - Higher = renders further but uses more memory
     - Recommended: 5-7 for most games
   
   - **Min Scale**: Minimum water scale
     - Lower = more detail up close, less distance
     - Recommended: 0.5-2
   
   - **Max Scale**: Maximum water scale
     - Higher = renders further, less detail up close
     - Set to 0 for unlimited
     - Recommended: 200-1000

### Inspector Location:
```
GameObject (with script)
└── Crest Lod Settings Controller (Component)
    ├── LOD Level Settings
    │   ├── Lod Levels: [Slider 1-15]
    │   └── Auto Find Water Renderer: [Checkbox]
    ├── Scale Range Settings
    │   ├── Min Scale: [Slider]
    │   ├── Max Scale: [Slider]
    │   └── Apply Scale Range: [Checkbox]
    └── Update Settings
        └── Update Every Frame: [Checkbox]
```

---

## Method 2: Directly on WaterRenderer Component

### Setup:
1. **Find WaterRenderer**:
   - In your scene hierarchy, find the GameObject with the **Water Renderer** component
   - This is usually the main water/ocean GameObject

2. **Open Inspector**:
   - Select the GameObject
   - Scroll down to find the **Water Renderer** component

3. **LOD Settings Location**:
   - Look for these sections in the Inspector:
     - **LOD Levels**: Number of detail levels (usually under "General" or "Simulation" section)
     - **Scale Range**: Min/Max scale values (usually under "General" or "Simulation" section)

### Inspector Location:
```
Water GameObject
└── Water Renderer (Component)
    ├── General / Simulation Section
    │   ├── Lod Levels: [Integer field]
    │   └── Scale Range: [Vector2 field - X = Min, Y = Max]
    └── ... (other settings)
```

---

## Understanding LOD Settings

### LOD Levels:
- **What it does**: Controls how many cascading detail levels are rendered
- **Effect**: 
  - More levels = water renders further away
  - Fewer levels = better performance, but water disappears closer
- **Typical Values**:
  - **Mobile/Low-end**: 3-5 levels
  - **PC/Medium**: 5-7 levels
  - **High-end/Open World**: 7-10 levels

### Scale Range:
- **Min Scale**: 
  - Controls the smallest the water can scale
  - Lower values = more detail when close to water
  - Higher values = better performance, less detail up close
  
- **Max Scale**:
  - Controls the largest the water can scale
  - Higher values = water renders further away
  - Set to 0 or very high = unlimited distance

### How Scale Works:
- Crest automatically scales water based on camera/viewpoint height
- When camera is high (flying), water scales up to show more area
- When camera is low (swimming), water scales down for detail
- Scale Range limits how much it can scale

---

## Recommended Settings by Use Case

### Close-Range Detail (Swimming, Boats):
```
LOD Levels: 5
Min Scale: 0.5
Max Scale: 50
```

### Medium Range (General Gameplay):
```
LOD Levels: 7
Min Scale: 1
Max Scale: 200
```

### Far Range (Open World, Flying):
```
LOD Levels: 10
Min Scale: 2
Max Scale: 1000
```

### Performance Optimized (Mobile):
```
LOD Levels: 4
Min Scale: 2
Max Scale: 100
```

---

## Tips

1. **Start with defaults** and adjust based on your needs
2. **Test at different camera heights** to see the effect
3. **Monitor performance** - more LOD levels = more memory usage
4. **Use the script** for runtime adjustments or easier access
5. **Check the Current Scale** in the script's debug info to see what's happening

---

## Troubleshooting

### Water disappears too close:
- **Solution**: Increase Min Scale or add more LOD Levels

### Water doesn't render far enough:
- **Solution**: Increase Max Scale or add more LOD Levels

### Performance issues:
- **Solution**: Reduce LOD Levels or increase Min Scale

### Can't find settings:
- **Solution**: Use the `CrestLodSettingsController` script - it's easier to find and use
