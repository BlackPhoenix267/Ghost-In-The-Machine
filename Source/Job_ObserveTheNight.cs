using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    // --- THE BRAIN ---
    public class JobGiver_ObserveTheNight : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;
            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            
            if (comp == null || !comp.isHighTierCore || comp.traits == null || !comp.traits.ContainsKey(GITM_DefOf.NightOwl)) return null;

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextObserveTheNightEligibleTick) return null;

            // Initialize tracker if this is the very first time
            if (!pawn.IsHashIntervalTick(60)) return null;

            bool isAurora = pawn.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.Aurora);
            int hour = GenLocalDate.HourInteger(pawn);
            bool isNightTime = hour >= 23 || hour < 3;

            // The job requires it to be night time OR an active Aurora
            if (!isNightTime && !isAurora) return null;

            // MTB calculation: Skip if Aurora is active, otherwise test the 2.5 day MTB
            if (!isAurora)
            {
                if (!Rand.MTBEventOccurs(2.5f, 60000f, 60f)) return null;
            }

            // Find a valid, unroofed spot to stand (same logic vanilla skygazing uses)
            if (!RCellFinder.TryFindSkygazeCell(pawn.Position, pawn, out IntVec3 cell))
            {
                return null;
            }
            // Log the attempt
            comp.nextObserveTheNightEligibleTick = currentTick + 60000;

            return JobMaker.MakeJob(GITM_DefOf.GITM_ObserveTheNight, cell);
        }
    }

    // --- THE BODY ---
    public class JobDriver_ObserveTheNight : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.Drafted);

            // Go to the designated unroofed spot
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            // Wait and observe
            Toil observe = ToilMaker.MakeToil("ObserveNight");
            observe.defaultCompleteMode = ToilCompleteMode.Delay;
            // 2-3 in-game hours (1 hour = 2500 ticks)
            observe.defaultDuration = Rand.Range(5000, 7500); 
            observe.tickAction = delegate
            {
                if (pawn.IsHashIntervalTick(400))
                {
                    pawn.rotationTracker.FaceCell(pawn.Position + GenAdj.AdjacentCellsAndInside[Rand.Range(0, 9)]);
                }
            };
            
            // Fail if someone builds a roof over them while they are watching
            observe.FailOn(() => pawn.Map.roofGrid.Roofed(pawn.Position)); 
            
            yield return observe;
        }
    }
}