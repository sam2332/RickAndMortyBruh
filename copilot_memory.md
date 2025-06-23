# Rick and Morty RimWorld Mod - Development Notes

## Mod Structure
- **Target**: RimWorld 1.5, C# 5 (.NET Framework 4.7.2)
- **Purpose**: Rick and Morty themed mod with portal gun mechanics

## Key Components

### Portal Gun System
1. **Portal Gun Item** (`RicksPortalGun.xml`): Weapon with portal verb capabilities
2. **Portal Verb** (`Verb_CastAbilityRickPortal.cs`): Handles teleportation and vaporization

### Auto-Portal Feature
- **Path Patch** (`RickPortalPathPatch.cs`): Auto-uses portal gun for long distances (working)

## Technical Details

### Portal Mechanics
- Manual use via right-click when portal gun equipped
- Auto-use when pathfinding long distances  
- Can teleport to locations or vaporize targeted pawns
- Range: 999 tiles, ignores line of sight and fog

### System Status
- Portal gun weapon: ✅ Working
- Auto-portal pathfinding: ✅ Working  
- Portal glove system: ❌ Removed (as requested)

## Recent Changes (v2.1)

### Portal Bug Fixes (Latest)
- **Fixed missing method**: Added `TryPortalTo(IntVec3)` overload to `CompApparelPortalGun` that was missing
- **Enhanced verb initialization**: Properly initialize `Verb_CastAbilityRickPortal` with verbProps when created programmatically
- **Added safety checks**: Null checks for caster and caster.Map in `CanHitTarget` and `CanHitTargetFrom` methods
- **Improved debugging**: More detailed logging in `UsePortalGun` method to track validation failures

### Debug Features Added
1. **Method Resolution**: Added missing `TryPortalTo(IntVec3)` method to apparel component
2. **Verb Initialization**: Proper initialization of verb properties for programmatic use
3. **Safety Validation**: Comprehensive null checks and validation logging
4. **Target Validation**: Step-by-step logging of target validation process

### Current Issue Resolution
- **Problem**: "AI Apparel TryPortalTo failed" due to missing method overload
- **Solution**: Added proper method overload and verb initialization
- **Status**: ✅ Should be fixed now

## Recent Changes (v2.0)

### Portal Auto-Teleport Debug (Latest)
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
