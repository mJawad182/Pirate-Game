# Weather Progression Controller - Setup Guide

## Overview
This script creates a cinematic weather progression system that:
1. Starts with **Cloudy 1** weather
2. Gradually transitions to **Cloudy 2** → **Cloudy 3** → **Storm** over 20-25 seconds
3. Activates **THOR Thunderstorm** just before the storm weather kicks in
4. Moves a Transform GameObject towards the player (Main Camera) at configurable speed

## Setup Instructions

### Step 1: Add the Script
1. Create an empty GameObject in your scene (or use an existing one)
2. Name it "Weather Progression Controller"
3. Add the `WeatherProgressionController` component to it

### Step 2: Configure Weather Settings

#### Weather Type Names
Make sure these match your Enviro weather type names exactly:
- **Cloudy 1**: `"Cloudy 1"` (default)
- **Cloudy 2**: `"Cloudy 2"` (default)
- **Cloudy 3**: `"Cloudy 3"` (default)
- **Storm**: `"Storm"` (default)

**To verify your weather type names:**
1. Select your Enviro Manager GameObject
2. In Inspector, go to **Weather Module** → **Settings** → **Weather Types**
3. Check the names of your weather types (they should match exactly)

#### Transition Duration
- **Total Transition Duration**: Set to `20-25` seconds (default: 22.5s)
- This controls how long the entire weather progression takes

#### Cloud Transition Speed
- **Cloud Transition Speed**: `0.1-2.0` (default: 0.5)
- **Lower values** = slower, more visible cloud formation
- **Higher values** = faster cloud transitions
- Recommended: `0.3-0.7` for visible gradual formation

### Step 3: Configure THOR Thunderstorm

#### Finding THOR Thunderstorm
The script will automatically try to find THOR Thunderstorm, but you can manually assign it:

1. In your scene, find the **THOR Thunderstorm** GameObject
   - It might be named: `"THOR_Thunderstorm"`, `"THOR Thunderstorm"`, or `"THOR_Thunderstorm(Clone)"`
2. Drag it into the **"Thor Thunderstorm Object"** field in the Inspector

#### THOR Settings
- **Thor Activation Point**: `0.85` (85% through progression)
  - `0.0` = activate at start
  - `1.0` = activate at end
  - `0.85` = activate just before storm (recommended)
  
- **Thor Intensity**: `0.0-1.0` (default: 1.0)
  - `0` = off
  - `1` = full intensity
  
- **Thor Transition Duration**: `1-10` seconds (default: 5s)
  - How long it takes THOR to fade in

### Step 4: Configure Transform Movement

#### Assign Transform to Move
1. Create or select a GameObject that you want to move towards the player
2. Drag it into the **"Transform To Move"** field

#### Movement Settings
- **Movement Speed**: `0.1-50` units per second (default: 5)
  - Adjust this to control how fast the object moves
  - You can change this value anytime in the Inspector
  
- **Target Transform**: Leave empty to use Main Camera automatically
  - Or assign a specific Transform to move towards
  
- **Stop Distance**: `0.1-10` units (default: 1)
  - Stops moving when this close to target

### Step 5: Test Settings

#### Debug Options
- **Start On Start**: ✅ Checked (starts automatically when game begins)
- **Show Debug**: ✅ Checked (shows console messages)

## How It Works

### Weather Progression Timeline
```
0s ────────────────────────────────────────── 22.5s
│
├─ Cloudy 1 (initial)
│
├─ Cloudy 2 (after ~7.5s)
│
├─ THOR Activated (after ~19s / 85%)
│
├─ Cloudy 3 (after ~15s)
│
└─ Storm (after ~22.5s)
```

### Cloud Formation
The script sets Enviro's `cloudsTransitionSpeed` to create gradual, visible cloud formation. Lower values make clouds form more slowly and visibly.

### THOR Activation
THOR Thunderstorm activates at 85% through the progression (just before storm weather), creating a dramatic buildup effect.

### Transform Movement
The assigned Transform GameObject continuously moves towards the player (Main Camera) at the specified speed, stopping when it reaches the stop distance.

## Usage Examples

### Example 1: Basic Setup
```
1. Add script to empty GameObject
2. Leave all defaults
3. Assign Transform to Move
4. Play game - weather progression starts automatically
```

### Example 2: Custom Timing
```
1. Set Total Transition Duration: 25 seconds
2. Set Cloud Transition Speed: 0.3 (slower, more visible)
3. Set Thor Activation Point: 0.9 (activate later)
4. Set Movement Speed: 3 (slower movement)
```

### Example 3: Manual Control
```
1. Uncheck "Start On Start"
2. Call StartWeatherProgression() from another script
3. Or call it from a button/trigger
```

## API Methods

### Public Methods
- `StartWeatherProgression()` - Starts the weather progression sequence
- `StopWeatherProgression()` - Stops the current progression
- `ResetWeatherProgression()` - Resets and restarts the progression

### Example: Start from Another Script
```csharp
WeatherProgressionController controller = FindObjectOfType<WeatherProgressionController>();
controller.StartWeatherProgression();
```

## Troubleshooting

### Weather Not Changing
- ✅ Check that weather type names match exactly (case-sensitive)
- ✅ Verify Enviro Manager is in the scene
- ✅ Check that Weather Module is enabled
- ✅ Enable "Show Debug" to see console messages

### THOR Not Activating
- ✅ Verify THOR Thunderstorm GameObject exists in scene
- ✅ Check that THOR_Thunderstorm component is on the GameObject
- ✅ Manually assign THOR GameObject in Inspector
- ✅ Check console for error messages

### Transform Not Moving
- ✅ Verify Transform To Move is assigned
- ✅ Check that Main Camera exists (or assign Target Transform)
- ✅ Verify Movement Speed > 0
- ✅ Check that Stop Distance isn't too large

### Clouds Forming Too Fast/Slow
- ✅ Adjust Cloud Transition Speed:
  - Slower = lower value (0.2-0.4)
  - Faster = higher value (0.8-1.5)

## Notes

- The script automatically finds Enviro Manager and Main Camera
- Weather progression runs once by default (can be restarted with ResetWeatherProgression())
- Cloud transition speed is set automatically when progression starts
- Transform movement continues until it reaches stop distance
- THOR Thunderstorm uses its built-in API for smooth activation

## Customization Tips

1. **Faster Weather**: Increase Cloud Transition Speed (0.8-1.2)
2. **Slower Weather**: Decrease Cloud Transition Speed (0.2-0.4)
3. **Earlier THOR**: Decrease Thor Activation Point (0.7-0.8)
4. **Later THOR**: Increase Thor Activation Point (0.9-0.95)
5. **Faster Movement**: Increase Movement Speed (10-20)
6. **Slower Movement**: Decrease Movement Speed (1-3)
