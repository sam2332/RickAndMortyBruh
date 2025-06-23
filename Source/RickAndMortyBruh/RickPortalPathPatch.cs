using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld.Planet;
using Verse.AI;
using RimWorld;
using HarmonyLib;

namespace RickAndMortyBruh
{
    [StaticConstructorOnStartup]
    public static class RickPortalPathPatch
    {
        static RickPortalPathPatch()
        {
            var harmony = new Harmony("RickAndMortyBruh.PortalPatch");
            harmony.Patch(
                original: AccessTools.Method(typeof(Pawn_PathFollower), "StartPath"),
                prefix: new HarmonyMethod(typeof(RickPortalPathPatch), "UsePortalFirst")
            );
        }  
        public static bool UsePortalFirst(Pawn_PathFollower __instance, IntVec3 dest, PathEndMode peMode)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return true;

            // Only apply to player faction pawns
            if (pawn.Faction != Faction.OfPlayer)
                return true;

            // Already there
            if (pawn.Position == dest)
                return false;

            // Check if destination is too close - no need for portal
            float distance = (pawn.Position - dest).LengthHorizontal;
            if (distance < 15f)
                return true;

            // Estimate path cost
            PathFinder pf = pawn.Map.pathFinder;
            PawnPath path = pf.FindPath(pawn.Position, dest, TraverseParms.For(pawn), PathEndMode.OnCell);
            if (path == null || path.NodesLeftCount == 0)
                return true;

            float estimatedCost = path.TotalCost;
            path.Dispose();            // Only use portal if path is expensive (>40 ticks) and distance is reasonable
            if (estimatedCost <= 40f || distance > 80f)
                return true;

            // Check if pawn has portal gun equipped
            if (pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.Primary.def.defName == "RickPortalGun")
            {
                // Get the portal gun's verb
                var portalVerb = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_CastAbilityRickPortal;
                if (portalVerb != null && portalVerb.Available())
                {
                    // Try to cast the portal to the destination
                    LocalTargetInfo target = new LocalTargetInfo(dest);
                    if (portalVerb.ValidateTarget(target, false))
                    {
                        if (portalVerb.TryStartCastOn(target))
                        {
                            Log.Message(string.Format("[Rick Portal] Auto-portal triggered for {0} to {1}", pawn.LabelShort, dest));
                            return false; // Skip normal walking
                        }
                    }
                }
            }

            return true;
        }
    }
}
