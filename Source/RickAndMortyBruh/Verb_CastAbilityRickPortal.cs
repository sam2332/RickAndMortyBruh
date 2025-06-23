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
            }
        }        public void InitializeFromAbilityDef(string defName)
        {
            abilityDef = defName;
        }        // Public method to manually trigger portal with a target
        public bool TryPortalTo(LocalTargetInfo target)
        {
            Log.Message(string.Format("[Rick Portal] TryPortalTo called with target: {0}, Cell: {1}", target, target.Cell));
            
            // Store the target in a private field and use it directly instead of relying on currentTarget
            manualTarget = target;
            useManualTarget = true;
            
            bool result = TryCastShot();
            
            // Reset manual target after use
            useManualTarget = false;
            manualTarget = LocalTargetInfo.Invalid;
            
            return result;
        }

        // Fields to store manual targeting
        private LocalTargetInfo manualTarget = LocalTargetInfo.Invalid;
        private bool useManualTarget = false;// Override validation to ignore line of sight and fog
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return TargetingValidator_IgnoreFog.ValidateTarget(target, this, showMessages);
        }

        // Override to ignore line of sight and fog checks
        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            bool result = false;
            
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
        }

        // Override to force visibility in fog
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            bool result = targ.Cell.InBounds(caster.Map);
            Log.Message("[Rick Portal] CanHitTargetFrom called: " + result);
            
            return result;
        }        protected override bool TryCastShot()
        {
            Pawn casterPawn = caster as Pawn;
            if (casterPawn == null)
            {
                Log.Warning("Portal gun failed: Caster is not a pawn.");                return false;
            }

            // Use manual target if available, otherwise fall back to CurrentTarget
            LocalTargetInfo effectiveTarget = useManualTarget ? manualTarget : CurrentTarget;
            
            Log.Message(string.Format("[Rick Portal] TryCastShot - EffectiveTarget: {0}, Cell: {1}, HasThing: {2}, UseManual: {3}", 
                effectiveTarget, effectiveTarget.Cell, effectiveTarget.HasThing, useManualTarget));            // Check if we're targeting a pawn directly
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
            
            Log.Message(string.Format("[Rick Portal] Teleportation - targetCell: {0}, caster position: {1}, map size: {2}x{3}", 
                targetCell, casterPawn.Position, casterPawn.Map.Size.x, casterPawn.Map.Size.z));

            if (targetCell.IsValid && targetCell.InBounds(casterPawn.Map))
            {
                // Check if there's a pawn at the target location and kill it first
                Pawn pawnAtTarget = targetCell.GetFirstPawn(casterPawn.Map);
                
                if (pawnAtTarget != null && pawnAtTarget != casterPawn)
                {
                    pawnAtTarget.Destroy(DestroyMode.KillFinalize);
                    
                    FleckMaker.ThrowSmoke(pawnAtTarget.Position.ToVector3(), pawnAtTarget.Map, 2.0f);
                    FleckMaker.ThrowMicroSparks(pawnAtTarget.Position.ToVector3(), pawnAtTarget.Map);
                    FleckMaker.ThrowLightningGlow(pawnAtTarget.Position.ToVector3(), pawnAtTarget.Map, 1.5f);
                    
                    Log.Message("Portal gun vaporized " + pawnAtTarget.LabelShort + " before teleporting");
                }

                if (targetCell.Standable(casterPawn.Map))
                {
                    casterPawn.Position = targetCell;
                    casterPawn.pather.StopDead();
                    casterPawn.Notify_Teleported();
                    FleckMaker.ThrowSmoke(targetCell.ToVector3(), casterPawn.Map, 1.0f);                    FleckMaker.ThrowMicroSparks(targetCell.ToVector3(), casterPawn.Map);
                    
                    Log.Message("Teleported " + casterPawn.LabelShort + " to " + targetCell);
                    
                    return true;
                }                else
                {
                    // Try to find the closest standable cell to the target, not a random one
                    IntVec3 standableCell = FindClosestStandableCell(targetCell, casterPawn.Map);
                    if (standableCell.IsValid)
                    {
                        casterPawn.Position = standableCell;
                        casterPawn.pather.StopDead();
                        casterPawn.Notify_Teleported();
                        FleckMaker.ThrowSmoke(standableCell.ToVector3(), casterPawn.Map, 1.0f);                        FleckMaker.ThrowMicroSparks(standableCell.ToVector3(), casterPawn.Map);
                        
                        Log.Message("Teleported " + casterPawn.LabelShort + " to " + standableCell + " (closest to target)");
                        
                        return true;
                    }
                }
            }

            Log.Warning("Portal gun failed: No valid target.");
            
            return false;
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
