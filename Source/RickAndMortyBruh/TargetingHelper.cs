using System;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace RickAndMortyBruh
{
    public static class TargetingHelper
    {
        public static TargetingParameters GetPortalGunTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetBuildings = false,
                validator = (TargetInfo target) =>
                {
                    return target.IsValid && target.Cell.InBounds(target.Map);
                }
            };
        }
    }

    public class CompAbilityEffect_Teleport : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            if (pawn != null && dest.IsValid && dest.Cell.InBounds(pawn.Map))
            {
                pawn.Position = dest.Cell;
                pawn.Notify_Teleported();
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) && dest.IsValid && dest.Cell.InBounds(parent.pawn.Map);
        }
    }

    
}
