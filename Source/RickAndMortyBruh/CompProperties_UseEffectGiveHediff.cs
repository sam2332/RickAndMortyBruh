using Verse;
using RimWorld;

namespace RickAndMortyBruh
{
    public class CompProperties_UseEffectGiveHediff : CompProperties_UseEffect
    {
        public HediffDef hediffDef;

        public CompProperties_UseEffectGiveHediff()
        {
            compClass = typeof(CompUseEffect_GiveHediff);
        }
    }

    public class CompUseEffect_GiveHediff : CompUseEffect
    {
        public CompProperties_UseEffectGiveHediff Props
        {
            get { return (CompProperties_UseEffectGiveHediff)props; }
        }

        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);
            if (Props.hediffDef != null)
            {
                user.health.AddHediff(Props.hediffDef);
            }
        }
    }
}
