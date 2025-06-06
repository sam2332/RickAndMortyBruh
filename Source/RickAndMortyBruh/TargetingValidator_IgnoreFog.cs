using Verse;
using RimWorld;
using RimWorld.Planet;

namespace RickAndMortyBruh
{
    public static class TargetingValidator_IgnoreFog
    {
        public static bool Validate(TargetInfo target)
        {
            return target.IsValid && target.Cell.InBounds(target.Map);
        }
    }
}
