using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    // --- THE BRAIN ---
    public class JobGiver_WatchOverLovedOne : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;

            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || comp.strongRelations == null || comp.strongRelations.Count == 0) return null;
            
            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextWatchOverLovedOnesEligibleTick) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            if (!Rand.MTBEventOccurs(5f, 60000f, 60f)) return null;

            // Find a sleeping loved one
            Pawn lovedOne = null;
            foreach (var kvp in comp.strongRelations)
            {
                if (kvp.Value >= 75)
                {
                    Pawn target = kvp.Key;
                    // Ensure they are on the map and actively asleep
                    if (target != null && target.Spawned && target.Map == pawn.Map && !target.Awake())
                    {
                        lovedOne = target;
                        break;
                    }
                }
            }

            if (lovedOne == null) return null;

            comp.nextWatchOverLovedOnesEligibleTick = currentTick + 60000;

            return JobMaker.MakeJob(GITM_DefOf.GITM_WatchOverTarget, lovedOne);
        }
    }

    // --- THE BODY ---
    public class JobDriver_WatchOverLovedOne : JobDriver
    {
        private const int WatchTicks = 5000;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // Go to the loved one
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Stare at them
            Toil watch = Toils_General.Wait(WatchTicks);
            watch.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
            };

            // Fail if they wake up:
            watch.AddFailCondition(() =>
            {
                Pawn targetPawn = TargetA.Thing as Pawn;
                return targetPawn != null && targetPawn.Awake();
            });

            // Apply thought when the toil ends (either naturally, or if the pawn wakes up)
            watch.AddFinishAction(delegate
            {
                // We ensure the mech actually watched for at least ~10 seconds.
                // If you drafted the mech immediately as they walked in, it aborts the thought.
                if (ticksLeftThisToil <= WatchTicks - 500)
                {
                    Pawn lovedOne = TargetA.Thing as Pawn;
                    if (lovedOne != null && lovedOne.needs != null && lovedOne.needs.mood != null)
                    {
                        lovedOne.needs.mood.thoughts.memories.TryGainMemory(GITM_DefOf.GITM_WatchedWhileSleeping);
                    }
                }
            });

            yield return watch;
        }
    }
}