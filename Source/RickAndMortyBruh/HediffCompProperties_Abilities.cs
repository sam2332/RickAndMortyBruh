using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace RickAndMortyBruh
{
    public class HediffCompProperties_Abilities : HediffCompProperties
    {
        public HediffCompProperties_Abilities()
        {
            compClass = typeof(HediffComp_Abilities);
        }
    }

    public class HediffComp_Abilities : HediffComp
    {
        public HediffCompProperties_Abilities Props
        {
            get { return (HediffCompProperties_Abilities)props; }
        }

        private Verb_CastAbilityRickPortal portalVerb;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            
            if (Pawn != null)
            {
                // Create the portal verb and initialize it properly
                portalVerb = new Verb_CastAbilityRickPortal();
                portalVerb.caster = Pawn;
                
                // Initialize verb properties manually
                VerbProperties verbProps = new VerbProperties();
                verbProps.verbClass = typeof(Verb_CastAbilityRickPortal);
                verbProps.range = 999f;
                verbProps.warmupTime = 0.5f;
                verbProps.targetParams = new TargetingParameters();
                verbProps.targetParams.canTargetLocations = true;
                verbProps.targetParams.canTargetPawns = true;
                verbProps.targetParams.canTargetBuildings = false;
                verbProps.targetParams.canTargetFires = false;
                verbProps.targetParams.neverTargetIncapacitated = false;
                
                portalVerb.verbProps = verbProps;
                
                Log.Message("[Rick Portal] Portal glove activated for " + Pawn.LabelShort);
            }
        }        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            Log.Message("[Rick Portal] CompGetGizmos called for hediff on " + (Pawn != null ? Pawn.LabelShort : "null pawn"));
            
            if (Pawn != null && Pawn.Faction == Faction.OfPlayer && portalVerb != null)
            {
                Log.Message("[Rick Portal] Creating portal gizmo for " + Pawn.LabelShort);
                
                Command_VerbTarget command = new Command_VerbTarget();
                command.defaultLabel = "Portal Gun";
                command.defaultDesc = "Open a portal to teleport or vaporize targets";
                command.icon = ContentFinder<Texture2D>.Get("Things/Item/Equipment/PortalGun", true);
                command.verb = portalVerb;
                
                // Make sure the verb is properly configured
                if (portalVerb.caster == null)
                {
                    portalVerb.caster = Pawn;
                }
                
                Log.Message("[Rick Portal] Portal gizmo created successfully");
                yield return command;
            }
            else
            {
                string pawnInfo = (Pawn != null ? Pawn.LabelShort : "null");
                string factionInfo = (Pawn != null && Pawn.Faction != null ? Pawn.Faction.Name : "null");
                string verbInfo = (portalVerb != null ? "exists" : "null");
                
                Log.Message("[Rick Portal] Gizmo not created - Pawn: " + pawnInfo + 
                          ", Faction: " + factionInfo + 
                          ", PortalVerb: " + verbInfo);
            }
        }
    }
}
