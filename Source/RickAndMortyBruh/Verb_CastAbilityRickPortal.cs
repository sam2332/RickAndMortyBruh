using System;
using Verse;
using RimWorld;
using System.Xml;

namespace RickAndMortyBruh
{
    public class Verb_CastAbilityRickPortal : Verb
    {
        public string abilityDef;
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            XmlNode abilityDefNode = xmlRoot.SelectSingleNode("abilityDef");
            if (abilityDefNode != null)
            {
                abilityDef = abilityDefNode.InnerText;
            }        }
        
        public void InitializeFromAbilityDef(string defName)
        {
            abilityDef = defName;
        }        // Public method to trigger portal to a destination during pathing
        public bool TryPortalTo(IntVec3 destination, Map map)
        {
            Log.Message(string.Format("[Rick Portal] Verb.TryPortalTo called with destination: {0} (x:{1},z:{2})", 
                destination, destination.x, destination.z));
            
            // Validate inputs first
            if (caster == null)
            {
                Log.Error("[Rick Portal] Verb.TryPortalTo: caster is null!");
                return false;
            }
            
            if (map == null)
            {
                Log.Error("[Rick Portal] Verb.TryPortalTo: map is null!");
                return false;
            }
            
            // Create a LocalTargetInfo from the destination cell
            LocalTargetInfo target = new LocalTargetInfo(destination);
            
            // Store the target in a private field and use it directly
            manualTarget = target;
            useManualTarget = true;
            
            Log.Message(string.Format("[Rick Portal] Stored manualTarget: {0} (x:{1},z:{2})", 
                manualTarget.Cell, manualTarget.Cell.x, manualTarget.Cell.z));
            
            Log.Message("[Rick Portal] About to call TryCastShot...");
            bool result = TryCastShot();
            Log.Message(string.Format("[Rick Portal] TryCastShot returned: {0}", result));
            
            // Reset manual target after use
            useManualTarget = false;
            manualTarget = LocalTargetInfo.Invalid;
            
            return result;
        }
        
        // Overload for LocalTargetInfo (keeping for compatibility)
        public bool TryPortalTo(LocalTargetInfo target)
        {
            return TryPortalTo(target.Cell, caster.Map);
        }
        
        // Fields to store manual targeting
        private LocalTargetInfo manualTarget = LocalTargetInfo.Invalid;
        private bool useManualTarget = false;
        
        // Override validation to ignore line of sight and fog
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return TargetingValidator_IgnoreFog.ValidateTarget(target, this, showMessages);
        }        // Override to ignore line of sight and fog checks
        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            bool result = false;
            
            // Safety check for caster and map
            if (caster == null || caster.Map == null)
            {
                Log.Warning("[Rick Portal] CanHitTarget: Caster or caster.Map is null");
                return false;
            }
            
            // Allow targeting pawns directly
            if (targ.HasThing && targ.Thing is Pawn)
            {
                result = true;
            }
            // Allow targeting valid cells
            else if (targ.IsValid && targ.Cell.InBounds(caster.Map))
            {
                result = true;
            }
              Log.Message("[Rick Portal] CanHitTarget called: " + result);
            
            return result;
        }        // Override to force visibility in fog
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            // Safety check for caster and map
            if (caster == null || caster.Map == null)
            {
                Log.Warning("[Rick Portal] CanHitTargetFrom: Caster or caster.Map is null");
                return false;
            }
            
            bool result = targ.Cell.InBounds(caster.Map);
            Log.Message("[Rick Portal] CanHitTargetFrom called: " + result);
            
            return result;
        }        protected override bool TryCastShot()
        {
            Log.Message("[Rick Portal] TryCastShot: Method entry");
            
            Pawn casterPawn = caster as Pawn;
            if (casterPawn == null)
            {
                Log.Warning("Portal gun failed: Caster is not a pawn.");
                return false;
            }

            Log.Message(string.Format("[Rick Portal] TryCastShot: Caster is {0}", casterPawn.LabelShort));

            // Use manual target if available, otherwise fall back to CurrentTarget
            LocalTargetInfo effectiveTarget = useManualTarget ? manualTarget : CurrentTarget;
            
            // Validate target before proceeding
            if (!effectiveTarget.IsValid)
            {
                Log.Warning("Portal gun failed: Invalid target.");
                return false;
            }
            
            Log.Message(string.Format("[Rick Portal] TryCastShot - EffectiveTarget: {0}, Cell: {1}, HasThing: {2}, UseManual: {3}", 
                effectiveTarget, effectiveTarget.Cell, effectiveTarget.HasThing, useManualTarget));// Check if we're targeting a pawn directly
            if (effectiveTarget.HasThing && effectiveTarget.Thing is Pawn)
            {
                Pawn targetPawn = effectiveTarget.Thing as Pawn;
                // Kill the targeted pawn instantly
                targetPawn.Destroy(DestroyMode.KillFinalize);
                
                // Add some visual effects
                FleckMaker.ThrowSmoke(targetPawn.Position.ToVector3(), targetPawn.Map, 2.0f);
                FleckMaker.ThrowMicroSparks(targetPawn.Position.ToVector3(), targetPawn.Map);                FleckMaker.ThrowLightningGlow(targetPawn.Position.ToVector3(), targetPawn.Map, 1.5f);
                
                Log.Message("Portal gun vaporized " + targetPawn.LabelShort);
                
                return true;
            }            // Otherwise, teleport the caster to the target location
            IntVec3 targetCell = effectiveTarget.Cell;
            
            Log.Message(string.Format("[Rick Portal] Raw target cell: {0} (x:{1}, z:{2})", 
                targetCell, targetCell.x, targetCell.z));
            
            // Clamp target cell to map bounds if it's outside
            if (targetCell.x < 0) targetCell.x = 0;
            if (targetCell.z < 0) targetCell.z = 0;
            if (targetCell.x >= casterPawn.Map.Size.x) targetCell.x = casterPawn.Map.Size.x - 1;
            if (targetCell.z >= casterPawn.Map.Size.z) targetCell.z = casterPawn.Map.Size.z - 1;
            
            Log.Message(string.Format("[Rick Portal] Clamped target cell: {0} (x:{1}, z:{2})", 
                targetCell, targetCell.x, targetCell.z));
            
            // Validate target cell
            if (!targetCell.IsValid)
            {
                Log.Warning("Portal gun failed: Target cell is invalid.");
                return false;
            }
            
            if (!targetCell.InBounds(casterPawn.Map))
            {
                Log.Warning(string.Format("Portal gun failed: Target cell {0} is out of bounds (map size: {1}x{2}).", 
                    targetCell, casterPawn.Map.Size.x, casterPawn.Map.Size.z));
                return false;
            }Log.Message(string.Format("[Rick Portal] Teleportation - targetCell: {0}, caster position: {1}, map size: {2}x{3}", 
                targetCell, casterPawn.Position, casterPawn.Map.Size.x, casterPawn.Map.Size.z));            // Check if there's a pawn at the target location and kill it first
            Pawn pawnAtTarget = targetCell.GetFirstPawn(casterPawn.Map);
            
            if (pawnAtTarget != null && pawnAtTarget != casterPawn)
            {
                pawnAtTarget.Destroy(DestroyMode.KillFinalize);
                
                FleckMaker.ThrowSmoke(pawnAtTarget.Position.ToVector3(), pawnAtTarget.Map, 2.0f);
                FleckMaker.ThrowMicroSparks(pawnAtTarget.Position.ToVector3(), pawnAtTarget.Map);
                FleckMaker.ThrowLightningGlow(pawnAtTarget.Position.ToVector3(), pawnAtTarget.Map, 1.5f);
                
                Log.Message("Portal gun vaporized " + pawnAtTarget.LabelShort + " before teleporting");
            }            // Properly teleport using RimWorld's spawn system
            IntVec3 finalTargetCell = targetCell;
            
            // If target cell is not standable, find the closest standable cell
            if (!targetCell.Standable(casterPawn.Map))
            {
                finalTargetCell = FindClosestStandableCell(targetCell, casterPawn.Map);
                if (!finalTargetCell.IsValid)
                {
                    Log.Warning("Portal gun failed: No standable cell found near target.");
                    return false;
                }
            }
            
            // Store original state
            Map map = casterPawn.Map;
            Rot4 originalRotation = casterPawn.Rotation;
              Log.Message(string.Format("[Rick Portal] Teleporting {0} from {1} to {2}", 
                casterPawn.LabelShort, casterPawn.Position, finalTargetCell));
            
            try
            {
                // DeSpawn the pawn temporarily
                Log.Message("[Rick Portal] About to DeSpawn pawn...");
                casterPawn.DeSpawn();
                Log.Message("[Rick Portal] Pawn DeSpawned successfully");
                
                // Respawn at new location
                Log.Message(string.Format("[Rick Portal] About to GenSpawn.Spawn at {0}...", finalTargetCell));
                GenSpawn.Spawn(casterPawn, finalTargetCell, map, originalRotation);
                Log.Message("[Rick Portal] Pawn spawned successfully");
                
                // Handle post-teleportation cleanup
                Log.Message("[Rick Portal] Calling post-teleportation cleanup...");
                casterPawn.pather.StopDead();
                casterPawn.Notify_Teleported();
                Log.Message("[Rick Portal] Post-teleportation cleanup completed");
                
                // Visual effects
                FleckMaker.ThrowSmoke(finalTargetCell.ToVector3(), map, 1.0f);
                FleckMaker.ThrowMicroSparks(finalTargetCell.ToVector3(), map);
                
                if (finalTargetCell != targetCell)
                {
                    Log.Message("Teleported " + casterPawn.LabelShort + " to " + finalTargetCell + " (closest to target " + targetCell + ")");
                }
                else
                {
                    Log.Message("Teleported " + casterPawn.LabelShort + " to " + finalTargetCell);
                }
                
                return true;            }
            catch (Exception e)
            {
                Log.Error(string.Format("[Rick Portal] Exception during teleportation: {0}", e));
                return false;
            }
        }

        // Helper method to find the closest standable cell to the target
        private IntVec3 FindClosestStandableCell(IntVec3 targetCell, Map map)
        {
            // First check if the target cell itself is standable
            if (targetCell.Standable(map))
            {
                return targetCell;
            }

            // Search in expanding rings around the target
            for (int radius = 1; radius <= 5; radius++)
            {
                IntVec3 bestCell = IntVec3.Invalid;
                float closestDistSq = float.MaxValue;

                // Check all cells in the current radius
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetCell, radius, true))
                {
                    if (!cell.InBounds(map) || !cell.Standable(map))
                        continue;

                    float distSq = (cell - targetCell).LengthHorizontalSquared;
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        bestCell = cell;
                    }
                }

                if (bestCell.IsValid)
                {
                    return bestCell;
                }
            }

            return IntVec3.Invalid;
        }
    }
}
