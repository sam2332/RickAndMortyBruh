using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace RickAndMortyBruh
{
    public class CompProperties_ApparelSpeedBoost : CompProperties
    {
        public float moveSpeedMultiplier = 1000000f; // insane speed multiplier

        public CompProperties_ApparelSpeedBoost()
        {
            compClass = typeof(CompApparelSpeedBoost);
        }
    }

    public class CompApparelSpeedBoost : ThingComp
    {
        public CompProperties_ApparelSpeedBoost Props
        {
            get
            {
                return (CompProperties_ApparelSpeedBoost)props;
            }
        }

        private Apparel Apparel
        {
            get
            {
                return parent as Apparel;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            
            // Only tick every 60 ticks (1 second) to reduce performance impact
            if (parent.IsHashIntervalTick(60))
            {
                CheckSpeedBoost();
            }
        }

        private void CheckSpeedBoost()
        {
            if (Apparel == null) return;
            
            Pawn wearer = Apparel.Wearer;
            if (wearer != null && wearer.Spawned)
            {
                // The speed boost is handled by the stat offset in the XML def
                // This is just for any additional effects or logging
                if (wearer.pather != null && wearer.pather.Moving)
                {
                    // Spawn green particles when moving at light speed!
                    Vector3 position = wearer.DrawPos;
                    
                    // Create a trail effect by spawning particles slightly behind movement direction
                    Vector3 backPosition = position - (wearer.pather.nextCell - wearer.Position).ToVector3() * 0.3f;
                    
                    // Throw green micro sparks behind the pawn as they move
                    FleckMaker.ThrowMicroSparks(backPosition, wearer.Map);
                    FleckMaker.ThrowMicroSparks(position, wearer.Map);
                    
                    // Add some green dust particles for extra effect
                    Color greenColor = new Color(0.1f, 0.8f, 0.2f, 0.7f);
                    FleckMaker.ThrowDustPuffThick(backPosition, wearer.Map, Rand.Range(0.8f, 1.5f), greenColor);
                    
                    // Add bright green sparks occasionally 
                    if (Rand.Chance(0.4f))
                    {
                        Color brightGreen = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                        FleckMaker.ThrowDustPuffThick(position, wearer.Map, 0.6f, brightGreen);
                    }
                    
                    // Occasionally throw some lightning-like effects for extra Rick & Morty science feel
                    if (Rand.Chance(0.2f))
                    {
                        FleckMaker.ThrowLightningGlow(position, wearer.Map, 0.8f);
                    }
                    
                    // Sometimes add extra micro sparks for a more intense effect
                    if (Rand.Chance(0.6f))
                    {
                        Vector3 randomOffset = new Vector3(Rand.Range(-0.2f, 0.2f), 0f, Rand.Range(-0.2f, 0.2f));
                        FleckMaker.ThrowMicroSparks(position + randomOffset, wearer.Map);
                    }
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Portal gun active");
            
            if (Apparel != null && Apparel.Wearer != null)
            {
                sb.Append(string.Format(" - {0} moves at light speed!", Apparel.Wearer.LabelShort));
                if (Apparel.Wearer.pather != null && Apparel.Wearer.pather.Moving)
                {
                    sb.Append(" *SPARKING WITH GREEN ENERGY*");
                }
            }
            
            return sb.ToString();
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (Apparel == null || Apparel.Wearer == null) yield break;

            // Add a fun gizmo button for flavor
            yield return new Command_Action
            {
                defaultLabel = "WUBBA LUBBA DUB DUB!",
                defaultDesc = "Express your excitement about moving at light speed!",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true),
                action = delegate
                {
                    Messages.Message(string.Format("{0}: WUBBA LUBBA DUB DUB! I'M MOVING AT LIGHT SPEED!", Apparel.Wearer.LabelShort), MessageTypeDefOf.PositiveEvent);
                }
            };
        }
    }
}
