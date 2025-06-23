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
        }        // Override validation to ignore line of sight and fog
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return TargetingValidator_IgnoreFog.ValidateTarget(target, this, showMessages);
        }        // Override to ignore line of sight and fog checks
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
        }        // Override to force visibility in fog
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            bool result = targ.Cell.InBounds(caster.Map);
            Log.Message("[Rick Portal] CanHitTargetFrom called: " + result);
            
            return result;
        }

        protected override bool TryCastShot()
        {
            Pawn casterPawn = caster as Pawn;
            if (casterPawn == null)
            {
                Log.Warning("Portal gun failed: Caster is not a pawn.");
                return false;
            }            // Check if we're targeting a pawn directly
            if (CurrentTarget.HasThing && CurrentTarget.Thing is Pawn)
            {
                Pawn targetPawn = CurrentTarget.Thing as Pawn;
                // Kill the targeted pawn instantly
                targetPawn.Destroy(DestroyMode.KillFinalize);
                
                // Add some visual effects
                FleckMaker.ThrowSmoke(targetPawn.Position.ToVector3(), targetPawn.Map, 2.0f);
                FleckMaker.ThrowMicroSparks(targetPawn.Position.ToVector3(), targetPawn.Map);
                FleckMaker.ThrowLightningGlow(targetPawn.Position.ToVector3(), targetPawn.Map, 1.5f);
                  Log.Message("Portal gun vaporized " + targetPawn.LabelShort);
                
                return true;
            }

            // Otherwise, teleport the caster to the target location
            IntVec3 targetCell = CurrentTarget.Cell;

            if (targetCell.IsValid && targetCell.InBounds(casterPawn.Map))            {
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

                if (targetCell.Standable(casterPawn.Map))                {
                    casterPawn.Position = targetCell;
                    casterPawn.pather.StopDead();
                    casterPawn.Notify_Teleported();
                    FleckMaker.ThrowSmoke(targetCell.ToVector3(), casterPawn.Map, 1.0f);
                    FleckMaker.ThrowMicroSparks(targetCell.ToVector3(), casterPawn.Map);             
                    Log.Message("Teleported " + casterPawn.LabelShort + " to " + targetCell);
                    
                    return true;
                }
                else
                {
                    IntVec3 standableCell;
                    if (CellFinder.TryFindRandomCellNear(targetCell, casterPawn.Map, 3, c => c.Standable(casterPawn.Map), out standableCell))                    {
                        casterPawn.Position = standableCell;
                        casterPawn.pather.StopDead();
                        casterPawn.Notify_Teleported();
                        FleckMaker.ThrowSmoke(standableCell.ToVector3(), casterPawn.Map, 1.0f);
                        FleckMaker.ThrowMicroSparks(standableCell.ToVector3(), casterPawn.Map);                        Log.Message("Teleported " + casterPawn.LabelShort + " to " + standableCell + " (near target)");
                        
                        return true;
                    }                }
            }            Log.Warning("Portal gun failed: No valid target.");
            
            return false;
        }
    }
}
