using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    // --- THE BRAIN ---
    public class JobGiver_GoUnderground : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;

            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || comp.traits == null) return null;

            // Must have the Undergrounder trait
            if (!comp.traits.ContainsKey(TraitDefOf.Undergrounder)) return null;

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextGoUndergroundEligibleTick) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            // MTB: 10 days
            if (!Rand.MTBEventOccurs(10f, 60000f, 60f)) return null;

            // Check if they are already under a mountain roof. If so, they are already happy.
            RoofDef currentRoof = pawn.Map.roofGrid.RoofAt(pawn.Position);
            if (currentRoof != null && currentRoof.isThickRoof) return null;

            // Try to find a cell with a thick mountain roof that the mech can reach
            if (!TryFindMountainRoofCell(pawn, out IntVec3 cell)) return null;
            
            comp.nextGoUndergroundEligibleTick = currentTick + 60000;

            return JobMaker.MakeJob(GITM_DefOf.GITM_GoUnderground, cell);
        }

        private bool TryFindMountainRoofCell(Pawn pawn, out IntVec3 result)
        {
            // Search for a valid cell under a thick rock roof
            return CellFinderLoose.TryFindRandomNotEdgeCellWith(10, (IntVec3 c) =>
            {
                if (!c.Standable(pawn.Map) || c.IsForbidden(pawn)) return false;

                // Ensure the mech can path to it and claim it
                if (!pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Some)) return false;

                RoofDef roof = pawn.Map.roofGrid.RoofAt(c);
                return roof != null && roof.isThickRoof;
            }, pawn.Map, out result);
        }
    }

    // --- THE BODY ---
    public class JobDriver_GoUnderground : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the specific tile so multiple mechs don't try to stand on the exact same spot
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // Go to the mountain roof cell
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            // 1 in-game hour is 2500 ticks.
            int waitDuration = Rand.Range(5000, 7500);

            // Stand and enjoy the rock ceiling
            Toil enjoyUnderground = Toils_General.Wait(waitDuration);

            // Add a fail condition in case the roof collapses or is removed by a mod while they stand there
            enjoyUnderground.FailOn(() =>
            {
                RoofDef roof = pawn.Map.roofGrid.RoofAt(pawn.Position);
                return roof == null || !roof.isThickRoof;
            });

            // Make them wander slightly within the cell or look around, so they don't look frozen
            enjoyUnderground.tickAction = delegate
            {
                if (pawn.IsHashIntervalTick(1000))
                {
                    pawn.rotationTracker.FaceCell(pawn.Position + GenAdj.AdjacentCellsAndInside[Rand.Range(0, 9)]);
                }
            };

            yield return enjoyUnderground;
        }
    }
}