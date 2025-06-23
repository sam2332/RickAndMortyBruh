# Rick and Morty RimWorld Mod - Development Notes

## Mod Structure
- **Target**: RimWorld 1.5, C# 5 (.NET Framework 4.7.2)
- **Purpose**: Rick and Morty themed mod with portal gun mechanics

## Key Components

### Portal Gun System
1. **Portal Gun Apparel** (`RicksPortalGun.xml`): Utility apparel with portal component
2. **Portal Component** (`CompApparelPortalGun.cs`): Apparel component for portal functionality
3. **Portal Verb** (`Verb_CastAbilityRickPortal.cs`): Handles teleportation and vaporization
4. **Auto-Portal Patch** (`RickPortalPathPatch.cs`): Auto-uses portal gun for long distances

## Current Status: PORTAL FAILURE IDENTIFIED

### Latest Debug Results (BREAKTHROUGH!)
From the screenshot logs, we can see the exact failure sequence:

```
[18:49:00] [Rick Portal] AI Distance 32.44936 >= 15 blocks, checking for portal gun...
[18:49:00] [Rick Portal] AI Portal gun detected! Path meets criteria for auto-portal.
[18:49:00] [Rick Portal] AI Using apparel portal gun component for dest: (68,0,146)
[18:49:00] [Rick Portal] CompApparelPortalGun.TryPortalTo(IntVec3) called with destination: (68,0,146)
[18:49:00] [Rick Portal] CompApparelPortalGun.TryPortalTo called with target: (68,0,146)
[18:49:00] [Rick Portal] CompApparelPortalGun.TryPortalTo: No wearer found
[18:49:00] [Rick Portal] AI Apparel TryPortalTo failed
```

### 🎯 ROOT CAUSE IDENTIFIED: NO WEARER FOUND

**THE PROBLEM**: `CompApparelPortalGun.TryPortalTo: No wearer found`

This means:
- ✅ Portal gun is detected correctly
- ✅ Auto-portal logic triggers correctly  
- ✅ Component methods are called correctly
- ❌ **FAILING**: `parent.ParentHolder as Pawn` returns null

### Analysis
The issue is in `CompApparelPortalGun.TryPortalTo()`:
```csharp
Pawn wearer = parent.ParentHolder as Pawn;
if (wearer == null)
{
    Log.Warning("[Rick Portal] CompApparelPortalGun.TryPortalTo: No wearer found");
    return false;  // <-- THIS IS WHERE IT FAILS
}
```

**Why `ParentHolder` is null:**
- The apparel component's `parent.ParentHolder` should be the pawn wearing it
- But it's returning null, suggesting the apparel isn't properly "worn" by the pawn
- This could be a timing issue or incorrect component access

### 🔧 SOLUTION IMPLEMENTED
Fixed the wearer detection issue by adding a new overload:

**CompApparelPortalGun.cs:**
```csharp
// New overload that accepts the pawn directly (for when ParentHolder fails)
public bool TryPortalTo(IntVec3 destination, Pawn wearer)
{
    Log.Message(string.Format("[Rick Portal] CompApparelPortalGun.TryPortalTo(IntVec3, Pawn) called with destination: {0}, wearer: {1}", destination, wearer.LabelShort));
    LocalTargetInfo target = new LocalTargetInfo(destination);
    return UsePortalGun(wearer, target);
}
```

**RickPortalPathPatch.cs:**
```csharp
// Use the apparel component's portal method with pawn parameter
if (apparelPortalComp.TryPortalTo(localDest, pawn))
```

**Status**: ✅ Compiled successfully - ready for testing!

### Expected New Log Output
```
[Rick Portal] AI Using apparel portal gun component for dest: (68,0,146)
[Rick Portal] CompApparelPortalGun.TryPortalTo(IntVec3, Pawn) called with destination: (68,0,146), wearer: ColonistName
[Rick Portal] UsePortalGun called - wearer: ColonistName, target: ...
[Rick Portal] Target validation passed
[Rick Portal] CanHitTarget passed, attempting portal
[Rick Portal] Pawn spawned successfully
```

### Next Action
Test the portal gun now - the "No wearer found" error should be fixed!

### Major Code Changes Made

#### 1. Teleportation Method Fix (Previous Session)
- **FIXED**: Changed from direct `pawn.Position = targetCell` to proper RimWorld teleportation
- **Method**: DeSpawn → GenSpawn.Spawn → Notify_Teleported
- **Status**: ✅ Implemented correctly

#### 2. Extensive Debug Logging Added (Current Session)
Added comprehensive logging to ALL portal-related methods:

**CompApparelPortalGun.cs:**
- `TryPortalTo(LocalTargetInfo)`: Logs method calls, wearer info, results
- `TryPortalTo(IntVec3)`: Logs destination conversion
- `UsePortalGun()`: Logs target validation, verb creation, all steps

**Verb_CastAbilityRickPortal.cs:**
- `TryPortalTo(IntVec3, Map)`: Logs inputs, validation, TryCastShot calls
- `TryCastShot()`: Logs method entry, caster info, target details
- **Teleportation section**: Wrapped in try-catch with step-by-step logging
  - DeSpawn logging
  - GenSpawn.Spawn logging  
  - Post-teleportation cleanup logging
  - Exception handling

**Expected Log Output:**
```
[Rick Portal] CompApparelPortalGun.TryPortalTo(IntVec3) called with destination: (x,z)
[Rick Portal] CompApparelPortalGun.TryPortalTo called with target: LocalTargetInfo
[Rick Portal] UsePortalGun called - wearer: PawnName, target: ..., target.Cell: ...
[Rick Portal] Created portal verb instance
[Rick Portal] Initialized verb properties - about to validate target: ...
[Rick Portal] Target validation passed
[Rick Portal] CanHitTarget passed, attempting portal
[Rick Portal] Verb.TryPortalTo called with destination: ...
[Rick Portal] About to call TryCastShot...
[Rick Portal] TryCastShot: Method entry
[Rick Portal] TryCastShot: Caster is PawnName
[Rick Portal] About to DeSpawn pawn...
[Rick Portal] Pawn DeSpawned successfully
[Rick Portal] About to GenSpawn.Spawn at (x,z)...
[Rick Portal] Pawn spawned successfully
```

#### 3. Compilation Status
- **Status**: ✅ Compiled successfully with all logging
- **File**: `RickAndMortyBruh.dll` updated with debug version
- **Ready**: For testing with detailed logging output

### Next Steps
1. **Test in-game** to capture detailed log sequence
2. **Identify exact failure point** from comprehensive logging
3. **Fix specific issue** based on log analysis
4. **Remove excessive logging** once issue is resolved

### Technical Implementation Details

#### Portal Gun Mechanics
- **Type**: Utility apparel (not weapon)
- **Component**: `CompApparelPortalGun` 
- **Verb**: `Verb_CastAbilityRickPortal`
- **Auto-trigger**: Via `RickPortalPathPatch` for distances >15 blocks

#### Teleportation Process
1. **Target Validation**: Check bounds, standable cells
2. **DeSpawn**: Remove pawn from current position
3. **GenSpawn.Spawn**: Place pawn at new position with rotation
4. **Cleanup**: StopDead(), Notify_Teleported(), visual effects

#### Known Working Features
- ✅ Portal gun appears as utility apparel
- ✅ Gizmo button for manual portal use
- ✅ Auto-portal detection for long distances
- ✅ Target validation and clamping
- ✅ Vaporization of target pawns
- ❌ **FAILING**: Actual pawn teleportation (under investigation)

### Debug Commands
- **Compile**: `./compile_mod.bat`
- **Test Location**: RimWorld with mod loaded
- **Log Watch**: Look for "[Rick Portal]" prefix messages

## Code Architecture

### File Structure
```
Source/RickAndMortyBruh/
├── CompApparelPortalGun.cs      (Apparel component)
├── Verb_CastAbilityRickPortal.cs (Teleportation logic)  
├── RickPortalPathPatch.cs       (Auto-portal patch)
└── TargetingValidator_IgnoreFog.cs

Defs/ThingDefs_Rick/
└── RicksPortalGun.xml           (Apparel definition)
```

### Recent Bug Investigation
- **Hypothesis**: Exception during DeSpawn/Spawn process
- **Evidence**: "AI Apparel TryPortalTo failed" suggests UsePortalGun returns false
- **Solution**: Added try-catch around teleportation with detailed logging
- **Status**: Awaiting test results with new logging
- **Fixed syntax errors**: Removed C# 6+ null conditional operators (?.) for C# 5 compatibility
- **Added comprehensive debug logging**: Portal path patch now logs every step of the decision process
- **Fixed method access**: Added `TryPortalTo()` method to `Verb_CastAbilityRickPortal` for manual portal triggering
- **Enhanced path detection**: Improved weapon and verb detection logic in `RickPortalPathPatch`

### Debug Features Added
1. **Path Check Logging**: Shows distance, path cost, and decision reasoning
2. **Weapon Detection**: Logs what weapon the pawn has equipped
3. **Portal Verb Detection**: Logs when portal gun verb is found or not found
4. **Target Validation**: Shows if target passes validation checks

### Current Auto-Portal Conditions
- Distance must be >= 15 tiles
- Path cost must be > 40 ticks
- Distance must be <= 80 tiles  
- Pawn must have `RickPortalGun` equipped
- Target must pass validation and hit checks

### Portal Gloves Removal
- ❌ Removed `PortalGloveHediff.xml` - Portal glove hediff definition
- ❌ Removed `HediffCompProperties_Abilities.cs` - Hediff component for portal abilities
- ✅ Cleaned up mod to use only portal gun weapon system

### Current Portal Gun Features
- **Weapon Slot**: Takes up the primary weapon slot
- **Manual Use**: Right-click to target locations or pawns
- **Auto-Portal**: Automatically triggers for paths >40 ticks and distance 15-80 tiles
- **Teleportation**: Instant teleport to targeted location
- **Vaporization**: Instantly kills targeted pawns
- **No LoS Required**: Ignores line of sight and fog of war
- **Range**: 999 tiles (unlimited on same map)

### Working Components
1. `RickPortalGun` weapon definition with custom verb
2. `Verb_CastAbilityRickPortal` for portal mechanics
3. `RickPortalPathPatch` for auto-portal on long paths
4. `TargetingValidator_IgnoreFog` for ignoring fog/LoS
5. Various targeting helpers and patches

## Compilation
- Use: `cd "c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RickAndMortyBruh"; .\compile_mod.bat`
- Compiler: Visual C# 4.8.9232.0 for C# 5
- Target: .NET Framework 4.7.2
- ✅ Successfully compiles with C# 5 / .NET 4.7.2
- ✅ Compatible with RimWorld 1.5

# Portal Gun Changes - Pacifist-Friendly Update

## Problem
The portal gun was originally implemented as a weapon, which prevented pacifist pawns from equipping it due to RimWorld's restriction on pacifist pawns wielding weapons.

## Solution
Converted the portal gun from a weapon to an apparel item (utility belt worn on waist):

### New Portal Gun (Apparel Version)
- **DefName**: `RickPortalGunApparel`
- **Type**: Apparel (belt layer, waist body part)
- **Functionality**: Provides a gizmo (action button) that allows teleportation
- **Pacifist-Friendly**: ✅ Yes - can be worn by pacifist pawns
- **Auto-Path Integration**: ✅ Yes - automatically uses portal for long-distance movement

### Legacy Portal Gun (Weapon Version)
- **DefName**: `RickPortalGun` (marked as obsolete)
- **Type**: Weapon
- **Pacifist-Friendly**: ❌ No - pacifist pawns cannot equip
- **Status**: Kept for compatibility but deprecated

### Technical Implementation

#### CompApparelPortalGun.cs
- New component that provides portal gizmo when worn
- Static helper methods for checking if pawn has portal gun
- Creates temporary `Verb_CastAbilityRickPortal` for portal logic

#### RickPortalPathPatch.cs Updates
- Now checks for apparel portal gun first
- Falls back to weapon version for compatibility
- Simplified portal verb creation logic

#### Conversion Recipe
- Added crafting recipe to convert weapon version to apparel version
- Requires Crafting 8 skill and machining bench
- 1:1 conversion ratio

### Usage
1. Equip the `RickPortalGunApparel` on any pawn (including pacifists)
2. Use the "Portal gun" gizmo button for manual teleportation
3. Auto-teleportation works for movement distances ≥15 blocks

### Files Modified
- `CompApparelPortalGun.cs` (new)
- `RicksPortalGun.xml` (updated - added apparel version)
- `RickPortalPathPatch.cs` (updated - checks apparel first)
- `RecipeDefs_PortalGun.xml` (new - conversion recipe)
- `Scenarios_RickAndMorty.xml` (new - test scenario)
