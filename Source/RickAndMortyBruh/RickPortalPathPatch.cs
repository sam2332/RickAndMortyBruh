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
        }        public static bool UsePortalFirst(Pawn_PathFollower __instance, IntVec3 dest, PathEndMode peMode)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return true;

            // Only apply to player faction pawns
            if (pawn.Faction != Faction.OfPlayer)
                return true;            // Validate destination coordinates first
            if (!dest.IsValid || !dest.InBounds(pawn.Map))
            {
                Log.Message(string.Format("[Rick Portal] Invalid destination: {0} (x:{1}, z:{2}) - skipping portal check", 
                    dest, dest.x, dest.z));
                return true;
            }

            Log.Message(string.Format("[Rick Portal] PathPatch checking pawn {0} going to dest: {1} (x:{2}, z:{3})", 
                pawn.LabelShort, dest, dest.x, dest.z));

            // Already there
            if (pawn.Position == dest)
                return false;
            
            // Check distance - portal if greater than 15 blocks
            float distance = (pawn.Position - dest).LengthHorizontal;
            Log.Message(string.Format("[Rick Portal] Distance: {0}", distance));
            
            // Additional check for NaN distance
            if (float.IsNaN(distance) || float.IsInfinity(distance))
            {
                Log.Warning(string.Format("[Rick Portal] Invalid distance calculation (NaN or Infinity): {0} - skipping portal", distance));
                return true;
            }
            
            if (distance < 15f){
                Log.Message("[Rick Portal] Distance too short for portal (< 15 blocks)");
                return true;
            }

            Log.Message(string.Format("[Rick Portal] Distance {0} >= 15 blocks, checking for portal gun...", distance));

            // Check if pawn has portal gun worn as apparel (preferred) or equipped as weapon (legacy)
            CompApparelPortalGun portalComp = CompApparelPortalGun.GetPortalGunComp(pawn);
            bool hasPortalGun = portalComp != null;
            
            // Fallback: Check for old weapon version for compatibility
            if (!hasPortalGun && pawn.equipment != null && pawn.equipment.Primary != null)
            {
                if (pawn.equipment.Primary.def.defName == "RickPortalGun")
                {
                    hasPortalGun = true;
                    Log.Message("[Rick Portal] Found legacy weapon version of portal gun");
                }
            }

            if (hasPortalGun)
            {
                Log.Message("[Rick Portal] Portal gun detected! Path meets criteria for auto-portal.");
                  // Create a temporary verb to handle the portal logic
                Verb_CastAbilityRickPortal portalVerb = new Verb_CastAbilityRickPortal();
                portalVerb.caster = pawn;
                  LocalTargetInfo target = new LocalTargetInfo(dest);
                  
                Log.Message(string.Format("[Rick Portal] PathPatch - Portal verb created for dest: {0} (x:{1}, z:{2})", 
                    dest, dest.x, dest.z));
                Log.Message(string.Format("[Rick Portal] PathPatch - Target: {0}, target.Cell: {1} (x:{2}, z:{3})", 
                    target, target.Cell, target.Cell.x, target.Cell.z));
                
                bool targetValid = portalVerb.ValidateTarget(target, false);
                bool canHit = portalVerb.CanHitTarget(target);
                
                Log.Message(string.Format("[Rick Portal] Target valid: {0}, Can hit: {1}", targetValid, canHit));
                  if (targetValid && canHit)
                {
                    // Use our custom portal method with direct destination
                    if (portalVerb.TryPortalTo(dest, pawn.Map))
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
                Log.Message("[Rick Portal] No portal gun found (checked both apparel and weapon)");
            }

            return true;
        }
    }
}
