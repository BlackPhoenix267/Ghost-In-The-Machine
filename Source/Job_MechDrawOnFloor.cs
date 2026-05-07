using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    // --- THE BRAIN ---
    public class JobGiver_MechDrawOnFloor : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;
            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || comp.traits == null) return null;

            // Check if the mech has either the Child or Tortured Artist trait
            bool hasCreativeTrait = comp.traits.ContainsKey(GITM_DefOf.GITM_Child) || 
                                    comp.traits.ContainsKey(GITM_DefOf.TorturedArtist);
            
            if (!hasCreativeTrait) return null;

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextMechDrawOnFloorEligibleTick) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            // Math: 15 days MTB
            if (!Rand.MTBEventOccurs(15f, 60000f, 60f)) return null;

            // Find a valid cell nearby to draw on
            if (!CellFinder.TryFindRandomReachableNearbyCell(
                pawn.Position, 
                pawn.Map, 
                12f, 
                TraverseParms.For(pawn), 
                // Cell must be standable, not forbidden, reserveable, and ideally not already drawn on
                (IntVec3 c) => c.Standable(pawn.Map) && !c.IsForbidden(pawn) && pawn.CanReserve(c), 
                null, 
                out IntVec3 drawCell))
            {
                return null; // Nowhere to draw
            }

            comp.nextMechDrawOnFloorEligibleTick = currentTick + 60000;
            return JobMaker.MakeJob(GITM_DefOf.GITM_MechDrawOnFloor, drawCell);
        }
    }

    // --- THE BODY ---
    public class JobDriver_MechDrawOnFloor : JobDriver
    {
        private const int DrawTicks = 2000; // Time spent drawing

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the single cell they are drawing on
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // Walk to the cell
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            // Do the drawing
            Toil draw = Toils_General.Wait(DrawTicks);
            draw.WithProgressBarToilDelay(TargetIndex.A);
            draw.FailOnDespawnedOrNull(TargetIndex.A);
            
            // Periodically check if the spot was ruined or became dangerous
            draw.tickAction = delegate
            {
                if (!TargetA.Cell.Standable(pawn.Map))
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };

            // Spawn the vanilla drawing when finished
            draw.AddFinishAction(delegate
            {
                if (ticksLeftThisToil <= 0) // Only spawn if job wasn't interrupted
                {
                    ThingDef filthDef = DefDatabase<ThingDef>.GetNamedSilentFail("Filth_FloorDrawing");
                    
                    if (filthDef != null)
                    {
                        // Check if there's already a drawing here so they don't stack infinitely
                        if (TargetA.Cell.GetFirstThing(pawn.Map, filthDef) == null)
                        {
                            GenSpawn.Spawn(filthDef, TargetA.Cell, pawn.Map);
                        }
                    }
                }
            });

            yield return draw;
        }
    }
}