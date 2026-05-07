using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    // --- THE BRAIN ---
    public class JobGiver_ObserveDrugs : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;

            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || !comp.isHighTierCore || comp.traits == null) return null;

            // In Vanilla, Chemical Fascination is TraitDefOf.DrugDesire at Degree 2
            if (!comp.traits.TryGetValue(TraitDefOf.DrugDesire, out int degree) || degree < 2)
            {
                return null;
            }

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextObserveDrugsEligibleTick) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            // Math: Evaluate MTB. 15 days, 60000 ticks per day, against the exact time delta
            if (!Rand.MTBEventOccurs(15f, 60000f, 60f)) return null;

            // Find the closest stack of drugs that the mech can safely reach
            System.Predicate<Thing> validator = (Thing t) => t.def.IsDrug && !t.IsForbidden(pawn) && pawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Some);
            
            Thing drug = GenClosest.ClosestThingReachable(
                pawn.Position, 
                pawn.Map, 
                ThingRequest.ForGroup(ThingRequestGroup.Drug), 
                PathEndMode.ClosestTouch, 
                TraverseParms.For(pawn), 
                9999f, 
                validator
            );

            if (drug == null) return null;

            comp.nextObserveDrugsEligibleTick = currentTick + 60000;

            return JobMaker.MakeJob(GITM_DefOf.GITM_ObserveDrugs, drug);
        }
    }

    // --- THE BODY ---
    public class JobDriver_ObserveDrugs : JobDriver
    {
        private const int ObserveTicks = 2500; // About 1 in-game hour

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // We don't reserve the drugs. This allows colonists to still haul/consume them.
            // If someone takes the drugs away, the FailOnDespawnedNullOrForbidden will gracefully cancel the mech's job.
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // Walk to the drugs
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            // Stare at them longingly
            Toil observe = Toils_General.Wait(ObserveTicks);
            observe.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
            };

            yield return observe;
        }
    }
}