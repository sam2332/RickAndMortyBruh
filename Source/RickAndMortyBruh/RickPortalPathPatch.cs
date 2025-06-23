using System;
using System.Collections;
using System.Collections.Generic;
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
            
            try
            {
                // Hook into Pawn_PathFollower.StartPath method for AI pathfinding
                var startPathMethod = AccessTools.Method(typeof(Pawn_PathFollower), "StartPath", new Type[] { typeof(LocalTargetInfo), typeof(PathEndMode) });
                if (startPathMethod != null)
                {
                    harmony.Patch(
                        original: startPathMethod,
                        prefix: new HarmonyMethod(typeof(RickPortalPathPatch), "UsePortalFirst")
                    );
                    Log.Message("[Rick Portal] Successfully patched Pawn_PathFollower.StartPath");
                }
                else
                {
                    Log.Warning("[Rick Portal] Could not find Pawn_PathFollower.StartPath method");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("[Rick Portal] Failed to apply path patches: " + ex.Message);
            }
        }

        public static bool UsePortalFirst(Pawn_PathFollower __instance, LocalTargetInfo dest, PathEndMode peMode)        {
            // Get the pawn from the PathFollower instance using reflection
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return true;

            // Only apply to player faction pawns
            if (pawn.Faction != Faction.OfPlayer)
                return true;

            // Validate destination coordinates first - check for corruption
            if (!dest.IsValid || dest.Cell.x < -1000 || dest.Cell.x > 1000 || dest.Cell.z < -1000 || dest.Cell.z > 1000)
            {
                Log.Warning(string.Format("[Rick Portal] Invalid destination coordinates detected: {0} - skipping portal", dest.Cell));
                return true;
            }

            IntVec3 localDest = dest.Cell;            // Validate destination coordinates first
            if (!localDest.IsValid || !localDest.InBounds(pawn.Map))
            {
                Log.Message(string.Format("[Rick Portal] Invalid destination: {0} (x:{1}, z:{2}) - skipping portal check", 
                    localDest, localDest.x, localDest.z));
                return true;
            }

            // Additional safety check for pawn position
            if (!pawn.Position.IsValid || pawn.Position.x < -1000 || pawn.Position.x > 1000 || 
                pawn.Position.z < -1000 || pawn.Position.z > 1000)
            {
                Log.Warning(string.Format("[Rick Portal] Corrupted pawn position detected: {0} - skipping portal", pawn.Position));
                return true;
            }

            Log.Message(string.Format("[Rick Portal] AI PathPatch checking pawn {0} going to dest: {1} (x:{2}, z:{3})", 
                pawn.LabelShort, localDest, localDest.x, localDest.z));

            // Already there
            if (pawn.Position == localDest)
                return true; // Continue normal pathing
              // Check distance - portal if greater than 15 blocks
            float distance;
            try 
            {
                distance = (pawn.Position - localDest).LengthHorizontal;
            }
            catch (Exception e)
            {
                Log.Error(string.Format("[Rick Portal] Exception calculating distance: {0} - skipping portal", e.Message));
                return true;
            }
            
            Log.Message(string.Format("[Rick Portal] AI Distance: {0}", distance));
            
            // Additional check for NaN distance
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0)
            {
                Log.Warning(string.Format("[Rick Portal] Invalid distance calculation: {0} - skipping portal", distance));
                return true;
            }
            
            if (distance < 15f){
                Log.Message("[Rick Portal] AI Distance too short for portal (< 15 blocks)");
                return true;
            }

            Log.Message(string.Format("[Rick Portal] AI Distance {0} >= 15 blocks, checking for portal gun...", distance));

            // Check if pawn has portal gun worn as apparel (preferred) or equipped as weapon (legacy)
            CompApparelPortalGun portalComp = CompApparelPortalGun.GetPortalGunComp(pawn);
            bool hasPortalGun = portalComp != null;
            
            // Fallback: Check for old weapon version for compatibility
            if (!hasPortalGun && pawn.equipment != null && pawn.equipment.Primary != null)
            {
                if (pawn.equipment.Primary.def.defName == "RickPortalGun")
                {
                    hasPortalGun = true;
                    Log.Message("[Rick Portal] AI Found legacy weapon version of portal gun");
                }
            }

            if (hasPortalGun)
            {
                Log.Message("[Rick Portal] AI Portal gun detected! Path meets criteria for auto-portal.");                // Get the portal gun apparel
                Apparel portalGunApparel = pawn.apparel.WornApparel.FirstOrDefault(a => a.def.defName == "RickPortalGunApparel");
                if (portalGunApparel != null)
                {
                    // Check if the apparel has verbs for portal functionality
                    var verbComp = portalGunApparel.GetComp<CompEquippable>();
                    if (verbComp != null && verbComp.AllVerbs.Any())
                    {                        // Find a portal verb
                        var portalVerb = verbComp.AllVerbs.FirstOrDefault(v => v is Verb_CastAbilityRickPortal);
                        if (portalVerb != null)
                        {                            Verb_CastAbilityRickPortal castPortalVerb = portalVerb as Verb_CastAbilityRickPortal;
                            if (castPortalVerb != null)
                            {
                                // Final safety check before creating target
                                if (localDest.x < -1000 || localDest.x > 1000 || localDest.z < -1000 || localDest.z > 1000)
                                {
                                    Log.Error(string.Format("[Rick Portal] AI Destination coordinates corrupted before portal: {0} - aborting", localDest));
                                    return true;
                                }

                                LocalTargetInfo target = new LocalTargetInfo(localDest);

                            Log.Message(string.Format("[Rick Portal] AI PathPatch - Portal verb created for dest: {0} (x:{1}, z:{2})",
                                localDest, localDest.x, localDest.z));
                            Log.Message(string.Format("[Rick Portal] AI PathPatch - Target: {0}, target.Cell: {1} (x:{2}, z:{3})",
                                target, target.Cell, target.Cell.x, target.Cell.z));

                            if (portalVerb.ValidateTarget(target, false) && portalVerb.CanHitTarget(target))
                            {
                                // Use our custom portal method with direct destination
                                if (castPortalVerb.TryPortalTo(localDest, pawn.Map))
                                {
                                    Log.Message(string.Format("[Rick Portal] AI Auto-portal triggered for {0} to {1}", pawn.LabelShort, localDest));
                                    return false; // Skip normal pathing - pawn has been teleported
                                }
                                else
                                {
                                    Log.Message("[Rick Portal] AI TryPortalTo failed");                                }
                            }
                            else
                            {
                                Log.Message("[Rick Portal] AI Target validation or hit check failed");
                            }
                            }
                        }
                        else
                        {
                            Log.Message("[Rick Portal] AI Portal verb not found on apparel.");
                        }
                    }
                    else
                    {
                        Log.Message("[Rick Portal] AI No verbs component found on the portal gun.");
                    }
                }
                else
                {
                    Log.Message("[Rick Portal] AI Portal gun apparel not found on pawn.");
                }
            }
            else
            {
                Log.Message("[Rick Portal] AI No portal gun found (checked both apparel and weapon)");
            }

            return true; // Continue with normal pathfinding if portal failed or not applicable
        }
    }
}
