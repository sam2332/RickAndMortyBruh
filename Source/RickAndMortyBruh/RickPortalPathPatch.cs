using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using RimWorld.Planet; // for GlobalTargetInfo

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

            // Already there
            if (pawn.Position == dest)
                return false;

            // Estimate path cost
            PathFinder pf = pawn.Map.pathFinder;
            PawnPath path = pf.FindPath(pawn.Position, dest, TraverseParms.For(pawn), PathEndMode.OnCell);
            if (path == null || path.NodesLeftCount == 0)
                return true;

            float estimatedCost = path.TotalCost;
            path.Dispose();

            if (estimatedCost <= 30 || (pawn.Position - dest).LengthHorizontal > 60)
                return true;

            // Try to find and cast RickPortalAbility via reflection
            foreach (var comp in pawn.AllComps)
            {
                var abilitiesField = comp.GetType().GetField("Abilities");
                if (abilitiesField != null)
                {
                    var abilities = abilitiesField.GetValue(comp) as IEnumerable;
                    if (abilities != null)
                    {
                        foreach (var ability in abilities)
                        {
                            var defField = ability.GetType().GetField("def");
                            if (defField == null) continue;

                            var def = defField.GetValue(ability) as Def;
                            if (def == null || def.defName != "RickPortalAbility") continue;

                            var queueCast = ability.GetType().GetMethod("QueueCastingJob", new[] { typeof(GlobalTargetInfo) });
                            if (queueCast != null)
                            {
                                queueCast.Invoke(ability, new object[] { new GlobalTargetInfo(dest, pawn.Map) });
                                return false; // Skip normal walking
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
