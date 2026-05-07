using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    public class MentalState_RescuingPawn : MentalState
    {
        public Pawn targetPawn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref targetPawn, "targetPawn");
        }

        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);

            if (targetPawn == null || targetPawn.Dead || targetPawn.InBed() || !targetPawn.Spawned)
            {
                RecoverFromState();
            }
        }

        public class JobGiver_RescueLovedOne : ThinkNode_JobGiver
        {
            protected override Job TryGiveJob(Pawn pawn)
            {
                var state = pawn.MentalState as MentalState_RescuingPawn;
                if (state == null || state.targetPawn == null) return null;

                Pawn target = state.targetPawn;

                Building_Bed bed = RestUtility.FindBedFor(target, pawn, false, false);

                if (bed != null && pawn.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Rescue, target, bed);
                    job.count = 1;
                    return job;
                }

                return null;
            }
        }
    }
}