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
        }

        public void InitializeFromAbilityDef(string defName)
        {
            abilityDef = defName;
        }

        protected override bool TryCastShot()
        {
            Pawn casterPawn = caster as Pawn;
            if (casterPawn == null)
            {
                Log.Warning("Portal gun failed to teleport: Caster is not a pawn.");
                return false;
            }

            IntVec3 targetCell = CurrentTarget.Cell;

            if (targetCell.IsValid && targetCell.InBounds(casterPawn.Map))
            {
                if (targetCell.Standable(casterPawn.Map))
                {
                    casterPawn.Position = targetCell;
                    FleckMaker.ThrowSmoke(targetCell.ToVector3(), casterPawn.Map, 1.0f);
                    FleckMaker.ThrowMicroSparks(targetCell.ToVector3(), casterPawn.Map);
                    Log.Message("Teleported " + casterPawn.LabelShort + " to " + targetCell);
                    return true;
                }
                else
                {
                    IntVec3 standableCell;
                    if (CellFinder.TryFindRandomCellNear(targetCell, casterPawn.Map, 3, c => c.Standable(casterPawn.Map), out standableCell))
                    {
                        casterPawn.Position = standableCell;
                        FleckMaker.ThrowSmoke(standableCell.ToVector3(), casterPawn.Map, 1.0f);
                        FleckMaker.ThrowMicroSparks(standableCell.ToVector3(), casterPawn.Map);
                        Log.Message("Teleported " + casterPawn.LabelShort + " to " + standableCell + " (near target)");
                        return true;
                    }
                }
            }

            Log.Warning("Portal gun failed to teleport: No valid target cell.");
            return false;
        }
    }
}
