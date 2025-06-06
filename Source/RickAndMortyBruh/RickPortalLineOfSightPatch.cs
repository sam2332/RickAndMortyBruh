using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace RickAndMortyBruh
{
    [StaticConstructorOnStartup]
    public static class RickPortalLineOfSightPatch
    {
        // Flag to track when we're using portal gun
        public static bool isUsingPortalGun = false;        static RickPortalLineOfSightPatch()
        {
            var harmony = new Harmony("RickAndMortyBruh.PortalLoSPatch");
            
            try
            {
                // Patch Verb.TryFindShootLineFromTo - this is the most critical one
                var shootLineMethod = AccessTools.Method(typeof(Verb), "TryFindShootLineFromTo");
                if (shootLineMethod != null)
                {
                    harmony.Patch(
                        original: shootLineMethod,
                        prefix: new HarmonyMethod(typeof(RickPortalLineOfSightPatch), "TryFindShootLineFromToPrefix")
                    );
                    Log.Message("[Rick Portal] Successfully patched TryFindShootLineFromTo");
                }
                else
                {
                    Log.Warning("[Rick Portal] Could not find TryFindShootLineFromTo method");
                }
                
                Log.Message("[Rick and Morty Mod] Portal patches applied successfully");
            }
            catch (System.Exception ex)
            {
                Log.Error("[Rick Portal] Failed to apply patches: " + ex.Message);
            }
        }        // Patch for Verb.TryFindShootLineFromTo to ensure portal gun can always find a line
        public static bool TryFindShootLineFromToPrefix(Verb __instance, IntVec3 root, LocalTargetInfo targ, 
            ref ShootLine resultingLine, ref bool __result)
        {
            // Check if this is a portal gun verb
            if (__instance is Verb_CastAbilityRickPortal)
            {
                // Always provide a direct shoot line for portal gun
                resultingLine = new ShootLine(root, targ.Cell);
                __result = true;
                return false; // Skip original method
            }
            
            // Continue with original method for all other cases
            return true;
        }
    }
}
