using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    // --- THE BRAIN ---
    public class JobGiver_ObserveFire : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;

            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || comp.traits == null) return null;
            
            // Check if the source pawn was a Pyromaniac
            if (!comp.traits.ContainsKey(TraitDefOf.Pyromaniac)) return null;

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextObserveFireEligibleTick) return null;

            List<Thing> fires = pawn.Map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            if (fires.Count == 0) return null;

            Thing targetFire = null;
            float closestDistSq = 999999f;

            // Find the closest fire in the home area
            foreach (Thing fire in fires)
            {
                // Must be in the Home Area- turned off currently
                //if (!pawn.Map.areaManager.Home[fire.Position]) continue;

                // Must be safely reachable (Danger.Some prevents walking through extreme danger)
                if (!pawn.CanReach(fire, PathEndMode.ClosestTouch, Danger.Some)) continue;

                float distSq = pawn.Position.DistanceToSquared(fire.Position);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    targetFire = fire;
                }
            }

            if (targetFire == null) return null;

            comp.nextObserveFireEligibleTick = currentTick + 60000;

            return JobMaker.MakeJob(GITM_DefOf.GITM_ObserveFire, targetFire);
        }
    }

    // --- THE BODY ---
    public class JobDriver_ObserveFire : JobDriver
    {
        private const int WatchTicks = 5000;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // If colonists extinguish the fire, or it burns out naturally, abort the job
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // Go to the fire. 
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedOrNull(TargetIndex.A);

            // Stand and watch
            Toil watch = Toils_General.Wait(WatchTicks);
            watch.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
            };
            watch.FailOnDespawnedOrNull(TargetIndex.A);

            yield return watch;
        }
    }
}