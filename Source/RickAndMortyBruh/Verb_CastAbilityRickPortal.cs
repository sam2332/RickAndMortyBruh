using Verse;
using RimWorld;
using System.Xml;

namespace RickAndMortyBruh
{
    public class Verb_CastAbilityRickPortal : Verb
    {
        public string abilityDef; // Field to store the abilityDef

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
        }        // Custom targeting logic for teleportation
        protected override bool TryCastShot()
        {
            if (CurrentTarget.HasThing && CurrentTarget.Thing is Pawn targetPawn)
            {
                // Teleport the target pawn to a random nearby location
                IntVec3 newLocation = CellFinder.RandomClosewalkCellNear(targetPawn.Position, targetPawn.Map, 10);
                targetPawn.Position = newLocation;
                Log.Message("Teleported " + targetPawn.Name + " to " + newLocation);
                return true;
            }
            if (caster != null)
            {
                // Teleport the caster to a random nearby location
                IntVec3 newLocation = CellFinder.RandomClosewalkCellNear(caster.Position, caster.Map, 10);
                caster.Position = newLocation;
                Log.Message("Teleported caster to " + newLocation);
                return true;
            }

            Log.Warning("Portal gun failed to teleport: No valid target.");
            return false;
        }
    }
}
