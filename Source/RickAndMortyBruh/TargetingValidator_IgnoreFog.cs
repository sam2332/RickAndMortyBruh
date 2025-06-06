using Verse;
using RimWorld;
using RimWorld.Planet;

namespace RickAndMortyBruh
{
    public static class TargetingValidator_IgnoreFog
    {
        public static bool Validate(TargetInfo target)
        {
            // Only check if target is valid and in bounds, ignore line of sight and fog of war
            return target.IsValid && target.Cell.InBounds(target.Map);
        }        public static bool ValidateTarget(LocalTargetInfo target, Verb verb, bool showMessages = true)
        {
            Log.Message("[Rick Portal] ValidateTarget called for " + verb.GetType().Name);
            
            if (!target.IsValid)
            {
                if (showMessages)
                {
                    Messages.Message("Invalid target location.", MessageTypeDefOf.RejectInput, false);
                }
                Log.Message("[Rick Portal] Target invalid");
                return false;
            }
            
            // Allow targeting pawns directly
            if (target.HasThing && target.Thing is Pawn)
            {
                Log.Message("[Rick Portal] Target valid - targeting pawn");
                return true;
            }
            
            if (target.Cell.InBounds(verb.caster.Map))
            {
                Log.Message("[Rick Portal] Target valid - in bounds");
                return true; // Always valid if in bounds, ignoring LoS and fog
            }
            
            if (showMessages)
            {
                Messages.Message("Out of bounds target location.", MessageTypeDefOf.RejectInput, false);
            }
            Log.Message("[Rick Portal] Target invalid - out of bounds");
            return false;
        }
    }
}
