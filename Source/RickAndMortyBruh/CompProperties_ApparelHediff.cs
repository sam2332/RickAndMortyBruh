using Verse;
using RimWorld;

namespace RickAndMortyBruh
{
    public class CompProperties_ApparelHediff : CompProperties
    {
        public HediffDef hediffDef;

        public CompProperties_ApparelHediff()
        {
            compClass = typeof(CompApparel_GiveHediff);
        }
    }

    public class CompApparel_GiveHediff : ThingComp
    {
        public CompProperties_ApparelHediff Props
        {
            get { return (CompProperties_ApparelHediff)props; }
        }

        private Pawn currentWearer = null;

        public override void CompTick()
        {
            base.CompTick();
            
            // Check if apparel is being worn
            Apparel apparel = parent as Apparel;
            if (apparel != null)
            {
                Pawn wearer = apparel.Wearer;
                
                // If someone started wearing it
                if (wearer != null && currentWearer != wearer)
                {
                    currentWearer = wearer;
                    if (Props.hediffDef != null)
                    {
                        // Add the hediff
                        if (!wearer.health.hediffSet.HasHediff(Props.hediffDef))
                        {
                            wearer.health.AddHediff(Props.hediffDef);
                            Log.Message("[Rick Portal] Added " + Props.hediffDef.defName + " to " + wearer.LabelShort);
                        }
                    }
                }
                // If someone stopped wearing it
                else if (wearer == null && currentWearer != null)
                {
                    if (Props.hediffDef != null && currentWearer.health != null)
                    {
                        // Remove the hediff
                        Hediff hediff = currentWearer.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
                        if (hediff != null)
                        {
                            currentWearer.health.RemoveHediff(hediff);
                            Log.Message("[Rick Portal] Removed " + Props.hediffDef.defName + " from " + currentWearer.LabelShort);
                        }
                    }
                    currentWearer = null;
                }
            }
        }
    }
}
