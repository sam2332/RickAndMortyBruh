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
{    [StaticConstructorOnStartup]
    public static class RickPortalPathPatch
    {
        // Track recent portal attempts to prevent spam
        private static Dictionary<Pawn, int> lastPortalTick = new Dictionary<Pawn, int>();
        private static int PORTAL_COOLDOWN_TICKS = 300; // 5 seconds at 60 fps
        
        static RickPortalPathPatch()
        {
            var harmony = new Harmony("RickAndMortyBruh.PortalPatch");            try
            {
                // Try to hook into job assignment instead of pathfinding
                var startJobMethod = AccessTools.Method(typeof(Pawn_JobTracker), "StartJob", new Type[] { typeof(Job), typeof(JobCondition), typeof(ThinkNode), typeof(bool), typeof(bool), typeof(ThinkTreeDef), typeof(JobTag?), typeof(bool), typeof(bool) });
                if (startJobMethod != null)
                {
                    harmony.Patch(
                        original: startJobMethod,
                        prefix: new HarmonyMethod(typeof(RickPortalPathPatch), "CheckJobForPortal")
                    );
                    Log.Message("[Rick Portal] Successfully patched Pawn_JobTracker.StartJob");
                }
                else
                {
                    Log.Warning("[Rick Portal] Could not find Pawn_JobTracker.StartJob method");
                    
                    // Fallback: Hook into Pawn_PathFollower.StartPath method for AI pathfinding
                    var startPathMethod = AccessTools.Method(typeof(Pawn_PathFollower), "StartPath", new Type[] { typeof(LocalTargetInfo), typeof(PathEndMode) });
                    if (startPathMethod != null)
                    {
                        harmony.Patch(
                            original: startPathMethod,
                            prefix: new HarmonyMethod(typeof(RickPortalPathPatch), "UsePortalFirst")
                        );
                        Log.Message("[Rick Portal] Successfully patched Pawn_PathFollower.StartPath as fallback");
                    }
                    else
                    {
                        Log.Warning("[Rick Portal] Could not find Pawn_PathFollower.StartPath method either");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("[Rick Portal] Failed to apply path patches: " + ex.Message);
            }
        }        public static bool UsePortalFirst(Pawn_PathFollower __instance, LocalTargetInfo dest, PathEndMode peMode)        {
            // Get the pawn from the PathFollower instance using reflection
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return true;

            // Only apply to player faction pawns
            if (pawn.Faction != Faction.OfPlayer)
                return true;

            // Check cooldown to prevent portal spam
            int currentTick = Find.TickManager.TicksGame;
            if (lastPortalTick.ContainsKey(pawn) && currentTick - lastPortalTick[pawn] < PORTAL_COOLDOWN_TICKS)
            {
                return true; // Still on cooldown, use normal pathfinding
            }

            // Validate destination coordinates first - check for corruption
            if (!dest.IsValid || dest.Cell.x < -1000 || dest.Cell.x > 1000 || dest.Cell.z < -1000 || dest.Cell.z > 1000)
            {
                Log.Warning(string.Format("[Rick Portal] Invalid destination coordinates detected: {0} - skipping portal", dest.Cell));
                return true;
            }

            IntVec3 localDest = dest.Cell;            
            // Validate destination coordinates first
            if (!localDest.IsValid || !localDest.InBounds(pawn.Map))
            {
                return true; // Skip logging for short-range movements
            }

            // Additional safety check for pawn position
            if (!pawn.Position.IsValid || pawn.Position.x < -1000 || pawn.Position.x > 1000 || 
                pawn.Position.z < -1000 || pawn.Position.z > 1000)
            {
                Log.Warning(string.Format("[Rick Portal] Corrupted pawn position detected: {0} - skipping portal", pawn.Position));
                return true;
            }

            // Already there or very close
            if (pawn.Position == localDest || (pawn.Position - localDest).LengthHorizontalSquared <= 4)
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
            
            // Additional check for NaN distance
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0)
            {
                Log.Warning(string.Format("[Rick Portal] Invalid distance calculation: {0} - skipping portal", distance));
                return true;
            }            if (distance < 15f){
                Log.Message(string.Format("[Rick Portal] AI Distance too short for portal: {0} blocks", distance));
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
            }            if (hasPortalGun)
            {
                Log.Message("[Rick Portal] AI Portal gun detected! Path meets criteria for auto-portal.");
                
                // Try apparel version first (preferred)
                Apparel portalGunApparel = pawn.apparel.WornApparel.FirstOrDefault(a => a.def.defName == "RickPortalGunApparel");
                if (portalGunApparel != null)
                {
                    // Use the custom apparel component directly
                    CompApparelPortalGun apparelPortalComp = portalGunApparel.GetComp<CompApparelPortalGun>();
                    if (apparelPortalComp != null)
                    {
                        // Final safety check before portal
                        if (localDest.x < -1000 || localDest.x > 1000 || localDest.z < -1000 || localDest.z > 1000)
                        {
                            Log.Error(string.Format("[Rick Portal] AI Destination coordinates corrupted before portal: {0} - aborting", localDest));
                            return true;                        }                        Log.Message(string.Format("[Rick Portal] AI Using apparel portal gun component for dest: {0} (x:{1}, z:{2})",
                            localDest, localDest.x, localDest.z));

                        // Use the apparel component's portal method with pawn parameter
                        if (apparelPortalComp.TryPortalTo(localDest, pawn))
                        {
                            Log.Message(string.Format("[Rick Portal] AI Auto-portal triggered for {0} to {1}", pawn.LabelShort, localDest));
                            // Set cooldown to prevent immediate re-triggering
                            lastPortalTick[pawn] = currentTick;
                            return false; // Skip normal pathing - pawn has been teleported
                        }
                        else
                        {
                            Log.Message("[Rick Portal] AI Apparel TryPortalTo failed");
                        }
                    }
                    else
                    {
                        Log.Message("[Rick Portal] AI No apparel portal component found on the portal gun.");
                    }
                }
                // Fallback to weapon version for compatibility
                else if (pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.Primary.def.defName == "RickPortalGun")
                {
                    ThingWithComps portalGunWeapon = pawn.equipment.Primary;
                    var verbComp = portalGunWeapon.GetComp<CompEquippable>();
                    if (verbComp != null && verbComp.AllVerbs.Any())
                    {
                        // Find a portal verb
                        var portalVerb = verbComp.AllVerbs.FirstOrDefault(v => v is Verb_CastAbilityRickPortal);
                        if (portalVerb != null)
                        {
                            Verb_CastAbilityRickPortal castPortalVerb = portalVerb as Verb_CastAbilityRickPortal;
                            if (castPortalVerb != null)
                            {
                                // Final safety check before creating target
                                if (localDest.x < -1000 || localDest.x > 1000 || localDest.z < -1000 || localDest.z > 1000)
                                {
                                    Log.Error(string.Format("[Rick Portal] AI Destination coordinates corrupted before portal: {0} - aborting", localDest));
                                    return true;
                                }

                                LocalTargetInfo target = new LocalTargetInfo(localDest);

                                Log.Message(string.Format("[Rick Portal] AI Using weapon portal verb for dest: {0} (x:{1}, z:{2})",
                                    localDest, localDest.x, localDest.z));

                                if (portalVerb.ValidateTarget(target, false) && portalVerb.CanHitTarget(target))
                                {                                    // Use our custom portal method with direct destination
                                    if (castPortalVerb.TryPortalTo(localDest, pawn.Map))
                                    {
                                        Log.Message(string.Format("[Rick Portal] AI Auto-portal triggered for {0} to {1}", pawn.LabelShort, localDest));
                                        // Set cooldown to prevent immediate re-triggering
                                        lastPortalTick[pawn] = currentTick;
                                        return false; // Skip normal pathing - pawn has been teleported
                                    }
                                    else
                                    {
                                        Log.Message("[Rick Portal] AI Weapon TryPortalTo failed");
                                    }
                                }
                                else
                                {
                                    Log.Message("[Rick Portal] AI Target validation or hit check failed");
                                }
                            }
                        }
                        else
                        {
                            Log.Message("[Rick Portal] AI Portal verb not found on weapon.");
                        }
                    }
                    else
                    {
                        Log.Message("[Rick Portal] AI No verbs component found on the portal gun weapon.");
                    }
                }
                else
                {
                    Log.Message("[Rick Portal] AI Portal gun not found (neither apparel nor weapon).");
                }            }
            else
            {
                Log.Message("[Rick Portal] AI No portal gun found (checked both apparel and weapon)");
            }

            return true; // Continue with normal pathfinding if portal failed or not applicable
        }        public static bool CheckJobForPortal(Pawn_JobTracker __instance, Job job, JobCondition lastJobEndCondition, ThinkNode jobGiver, bool resumeCurJobAfterwards, bool cancelBusyStances, ThinkTreeDef thinkTree, JobTag? tag, bool fromQueue, bool canReturnToPool)
        {
            // Get the pawn from the JobTracker instance using reflection
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return true;

            // Only apply to player faction pawns
            if (pawn.Faction != Faction.OfPlayer)
                return true;

            // Check cooldown to prevent portal spam
            int currentTick = Find.TickManager.TicksGame;
            if (lastPortalTick.ContainsKey(pawn) && currentTick - lastPortalTick[pawn] < PORTAL_COOLDOWN_TICKS)
            {
                return true; // Still on cooldown, use normal job
            }

            // Only process movement jobs with a valid target
            if (job == null || !job.targetA.IsValid || (!job.targetA.HasThing && !job.targetA.Cell.IsValid))
                return true;

            // Skip if this is not a movement-related job
            if (job.def != JobDefOf.Goto && job.def != JobDefOf.GotoWander && job.def != JobDefOf.Wait_Wander)
                return true;

            IntVec3 destination;
            
            // Determine the final destination from the job
            if (job.targetA.HasThing)
            {
                destination = job.targetA.Thing.Position;
            }
            else if (job.targetA.Cell.IsValid)
            {
                destination = job.targetA.Cell;
            }
            else
            {
                return true; // No valid destination
            }

            // Validate destination coordinates
            if (!destination.IsValid || !destination.InBounds(pawn.Map))
            {
                return true;
            }

            // Check distance - portal if greater than 15 blocks
            float distance;
            try 
            {
                distance = (pawn.Position - destination).LengthHorizontal;
            }
            catch (Exception e)
            {
                Log.Error(string.Format("[Rick Portal] Exception calculating distance in job: {0} - skipping portal", e.Message));
                return true;
            }
            
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 15f)
            {
                return true; // Distance too short or invalid
            }            Log.Message(string.Format("[Rick Portal] Job-based portal check for {0} going to {1}, distance: {2}, jobDef: {3}", 
                pawn.LabelShort, destination, distance, job.def != null ? job.def.defName : "null"));

            // Check if pawn has portal gun
            CompApparelPortalGun portalComp = CompApparelPortalGun.GetPortalGunComp(pawn);
            if (portalComp == null)
            {
                // Fallback: Check for old weapon version
                if (pawn.equipment == null || pawn.equipment.Primary == null || pawn.equipment.Primary.def.defName != "RickPortalGun")
                    return true;
            }

            // Try to portal to destination
            Apparel portalGunApparel = pawn.apparel.WornApparel.FirstOrDefault(a => a.def.defName == "RickPortalGunApparel");
            if (portalGunApparel != null)
            {
                CompApparelPortalGun apparelPortalComp = portalGunApparel.GetComp<CompApparelPortalGun>();
                if (apparelPortalComp != null && apparelPortalComp.TryPortalTo(destination))
                {
                    Log.Message(string.Format("[Rick Portal] Job-based portal success for {0} to {1}", pawn.LabelShort, destination));
                    lastPortalTick[pawn] = currentTick;
                    
                    // Cancel the job since we teleported
                    return false;
                }
            }

            return true; // Continue with normal job if portal failed
        }
    }
}
