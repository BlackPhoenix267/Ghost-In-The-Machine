using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    public class JobDriver_CarryCorpseToRipscanner : JobDriver
    {
        private Corpse Corpse => (Corpse)job.GetTarget(TargetIndex.A).Thing;
        private Building_SubcoreScanner Scanner => (Building_SubcoreScanner)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve both the corpse and the scanner so no one else messes with them
            return pawn.Reserve(Corpse, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Scanner, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);

            // 1. Walk to the corpse
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            // 2. Pick it up
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: false);

            // 3. Walk to the scanner
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);

            // 4. Instantly perform the scan and drop the body
            Toil scanToil = new Toil();
            scanToil.initAction = delegate
            {
                CorpseScanUtility.PerformCorpseScan(Scanner, Corpse);
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
            };
            scanToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return scanToil;
        }
    }
}