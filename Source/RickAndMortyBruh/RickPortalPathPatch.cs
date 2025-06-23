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

            Log.Message(string.Format("[Rick Portal] PathPatch checking pawn {0} going to {1}", pawn.LabelShort, dest));            // Already there
            if (pawn.Position == dest)
                return false;
            
            // Check distance - portal if greater than 15 blocks
            float distance = (pawn.Position - dest).LengthHorizontal;
            Log.Message(string.Format("[Rick Portal] Distance: {0}", distance));
            if (distance < 15f)
            {
                Log.Message("[Rick Portal] Distance too short for portal (< 15 blocks)");
                return true;
            }

            Log.Message(string.Format("[Rick Portal] Distance {0} >= 15 blocks, checking for portal gun...", distance));

            // Check if pawn has portal gun equipped
            if (pawn.equipment != null && pawn.equipment.Primary != null)
            {
                Log.Message(string.Format("[Rick Portal] Pawn has weapon: {0}", pawn.equipment.Primary.def.defName));
                if (pawn.equipment.Primary.def.defName == "RickPortalGun")
                {
                    Log.Message("[Rick Portal] Portal gun detected! Path meets criteria for auto-portal.");
                    Verb_CastAbilityRickPortal portalVerb = null;

                    if (pawn.equipment.Primary.def.Verbs != null)
                    {
                        foreach (var verbEntry in pawn.equipment.Primary.def.Verbs)                        {
                            if (verbEntry.verbClass == typeof(Verb_CastAbilityRickPortal))
                            {
                                // Find the actual verb instance
                                var verbTracker = pawn.equipment.Primary.GetComp<CompEquippable>();
                                if (verbTracker != null && verbTracker.VerbTracker != null)
                                {
                                    foreach (var verb in verbTracker.VerbTracker.AllVerbs)
                                    {
                                        if (verb is Verb_CastAbilityRickPortal)
                                        {
                                            portalVerb = verb as Verb_CastAbilityRickPortal;
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }                    // Fallback - try primary verb if it's our type
                    if (portalVerb == null && pawn.equipment.PrimaryEq.PrimaryVerb is Verb_CastAbilityRickPortal)
                    {
                        portalVerb = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_CastAbilityRickPortal;
                    }
                    
                    if (portalVerb != null)
                    {
                        Log.Message("[Rick Portal] Portal verb found, validating target...");
                        // Try to cast the portal to the destination
                        LocalTargetInfo target = new LocalTargetInfo(dest);
                        bool targetValid = portalVerb.ValidateTarget(target, false);
                        bool canHit = portalVerb.CanHitTarget(target);
                        
                        Log.Message(string.Format("[Rick Portal] Target valid: {0}, Can hit: {1}", targetValid, canHit));
                        
                        if (targetValid && canHit)
                        {
                            // Use our custom portal method
                            if (portalVerb.TryPortalTo(target))
                            {
                                Log.Message(string.Format("[Rick Portal] Auto-portal triggered for {0} to {1}", pawn.LabelShort, dest));
                                return false; // Skip normal walking
                            }
                            else
                            {
                                Log.Message("[Rick Portal] TryPortalTo failed");
                            }
                        }
                        else
                        {
                            Log.Message("[Rick Portal] Target validation or hit check failed");
                        }
                    }
                    else
                    {
                        Log.Message("[Rick Portal] Portal verb not found");
                    }
                }
            }
            else
            {
                Log.Message("[Rick Portal] No equipment or primary weapon");
            }

            return true;
        }
    }
}
