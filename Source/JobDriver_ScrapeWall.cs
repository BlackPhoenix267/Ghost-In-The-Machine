using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    public class JobDriver_ScrapeWall : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the wall so other pawns don't try to deconstruct it while we bash it
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            
            // Go to the wall
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);

            // Bash the wall toil
            Toil scrape = ToilMaker.MakeToil("ScrapeWall");
            scrape.tickAction = delegate
            {
                // Trigger an attack roughly once per second (60 ticks)
                if (Find.TickManager.TicksGame % 60 == 0) 
                {
                    pawn.rotationTracker.FaceCell(TargetA.Cell);
                    
                    // Attack the wall (makes the thud noise and damages the wall)
                    pawn.meleeVerbs.TryMeleeAttack(TargetA.Thing, null, false);
                    
                    // 50% chance to suffer a small self-inflicted injury per bash
                    if (Rand.Chance(0.5f)) 
                    {
                        // 1 to 3 blunt damage applied to the mech itself
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, Rand.Range(1, 4), 0, -1, pawn);
                        pawn.TakeDamage(dinfo);
                    }
                }
            };
            
            scrape.defaultCompleteMode = ToilCompleteMode.Delay;
            scrape.defaultDuration = job.expiryInterval > 0 ? job.expiryInterval : 400;
            
            yield return scrape;
        }
    }
}