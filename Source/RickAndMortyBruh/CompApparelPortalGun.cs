using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;

namespace RickAndMortyBruh
{
    public class CompProperties_ApparelPortalGun : CompProperties
    {
        public CompProperties_ApparelPortalGun()
        {
            compClass = typeof(CompApparelPortalGun);
        }
    }

    public class CompApparelPortalGun : ThingComp
    {
        public CompProperties_ApparelPortalGun Props
        {
            get
            {
                return (CompProperties_ApparelPortalGun)props;
            }
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (parent.Spawned)
            {
                Pawn wearer = parent.ParentHolder as Pawn;
                if (wearer != null && wearer.IsColonistPlayerControlled)
                {
                    yield return new Command_Target
                    {
                        defaultLabel = "Portal gun",
                        defaultDesc = "Teleport to target location or vaporize target pawn",
                        icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/Attack", true),
                        targetingParams = new TargetingParameters
                        {
                            canTargetLocations = true,
                            canTargetPawns = true,
                            canTargetBuildings = false,
                            canTargetItems = false
                        },
                        action = delegate(LocalTargetInfo target)
                        {
                            UsePortalGun(wearer, target);
                        }
                    };
                }
            }
        }        private bool UsePortalGun(Pawn wearer, LocalTargetInfo target)
        {
            Log.Message(string.Format("[Rick Portal] UsePortalGun called with target: {0}, Cell: {1} (x:{2},y:{3},z:{4}), IsValid: {5}, HasThing: {6}", 
                target, target.Cell, target.Cell.x, target.Cell.y, target.Cell.z, target.IsValid, target.HasThing));
            
            // Validate that we have reasonable coordinates
            if (!target.IsValid || !target.Cell.InBounds(wearer.Map))
            {
                Log.Warning(string.Format("[Rick Portal] Invalid target coordinates: {0}, map size: {1}x{2}", 
                    target.Cell, wearer.Map.Size.x, wearer.Map.Size.z));
                Messages.Message("Invalid target location", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            
            // Create a temporary verb to handle the portal logic
            Verb_CastAbilityRickPortal portalVerb = new Verb_CastAbilityRickPortal();
            
            // Properly initialize the verb
            portalVerb.caster = wearer;
            portalVerb.verbProps = new VerbProperties();
            portalVerb.verbProps.range = 999f;
            portalVerb.verbProps.targetParams = new TargetingParameters();
            portalVerb.verbProps.targetParams.canTargetLocations = true;
            portalVerb.verbProps.targetParams.canTargetPawns = true;
            portalVerb.verbProps.targetParams.canTargetBuildings = false;
            
            Log.Message(string.Format("[Rick Portal] About to validate and use portal with target: {0} (x:{1},y:{2},z:{3})", 
                target.Cell, target.Cell.x, target.Cell.y, target.Cell.z));
              
            // Use the existing portal logic - don't set currentTarget manually, let TryPortalTo handle it
            if (portalVerb.ValidateTarget(target, true))
            {
                Log.Message("[Rick Portal] Target validation passed");
                if (portalVerb.CanHitTarget(target))
                {
                    Log.Message("[Rick Portal] CanHitTarget passed, attempting portal");
                    bool result = portalVerb.TryPortalTo(target.Cell, wearer.Map);
                    Log.Message(string.Format("[Rick Portal] Portal attempt result: {0}", result));
                    return result;
                }
                else
                {
                    Log.Warning("[Rick Portal] CanHitTarget failed");
                    Messages.Message("Cannot portal to that location", MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            else
            {
                Log.Warning("[Rick Portal] Target validation failed");
                Messages.Message("Cannot portal to that location", MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }

        // Helper method to check if a pawn has the portal gun equipped
        public static bool HasPortalGun(Pawn pawn)
        {
            if (pawn == null || pawn.apparel == null || pawn.apparel.WornApparel == null)
                return false;

            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel.def.defName == "RickPortalGunApparel")
                {
                    return true;
                }
            }
            return false;
        }        // Helper method to get the portal gun comp from a pawn
        public static CompApparelPortalGun GetPortalGunComp(Pawn pawn)
        {
            if (pawn == null || pawn.apparel == null || pawn.apparel.WornApparel == null)
                return null;

            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel.def.defName == "RickPortalGunApparel")
                {
                    return apparel.GetComp<CompApparelPortalGun>();
                }
            }
            return null;        }        // Public method to trigger portal teleportation from code
        public bool TryPortalTo(LocalTargetInfo target)
        {
            Log.Message(string.Format("[Rick Portal] CompApparelPortalGun.TryPortalTo called with target: {0}", target));
            
            Pawn wearer = parent.ParentHolder as Pawn;
            if (wearer == null)
            {
                Log.Warning("[Rick Portal] CompApparelPortalGun.TryPortalTo: No wearer found");
                return false;
            }

            Log.Message(string.Format("[Rick Portal] CompApparelPortalGun.TryPortalTo: Wearer is {0}, calling UsePortalGun", wearer.LabelShort));
            bool result = UsePortalGun(wearer, target);
            Log.Message(string.Format("[Rick Portal] CompApparelPortalGun.TryPortalTo: UsePortalGun returned {0}", result));
            return result;
        }

        // Overload for IntVec3 destination
        public bool TryPortalTo(IntVec3 destination)
        {
            Log.Message(string.Format("[Rick Portal] CompApparelPortalGun.TryPortalTo(IntVec3) called with destination: {0}", destination));
            LocalTargetInfo target = new LocalTargetInfo(destination);
            return TryPortalTo(target);
        }
        
        // Overload that accepts the pawn directly (for when ParentHolder fails)
        public bool TryPortalTo(IntVec3 destination, Pawn wearer)
        {
            Log.Message(string.Format("[Rick Portal] CompApparelPortalGun.TryPortalTo(IntVec3, Pawn) called with destination: {0}, wearer: {1}", destination, wearer.LabelShort));
            LocalTargetInfo target = new LocalTargetInfo(destination);
            return UsePortalGun(wearer, target);
        }
    }
}
