using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    // --- THE BRAIN ---
    public class JobGiver_ObserveFood : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;
            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || comp.traits == null || comp.traits.Count == 0) return null;

            if (!comp.traits.ContainsKey(GITM_DefOf.Gourmand)) return null;

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextObserveFoodEligibleTick) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            if (!Rand.MTBEventOccurs(15f, 60000f, 60f)) return null;

            // Find the nearest food item. 
            Thing food = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree),
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                9999f,
                val => val.def.IsNutritionGivingIngestible && !val.IsForbidden(pawn)
            );

            if (food == null) return null;

            comp.nextObserveFoodEligibleTick = currentTick + 60000;

            return JobMaker.MakeJob(GITM_DefOf.GITM_ObserveFood, food);
        }
    }

    // --- THE BODY ---
    public class JobDriver_ObserveFood : JobDriver
    {
        private int waitTicks;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref waitTicks, "waitTicks", 0);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            waitTicks = Rand.Range(5000, 7500);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Does not reserve the food. If a colonist comes by and eats it, 
            // the mech's job will just naturally fail and they'll go back to work.
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail if the food rots away, gets eaten, or is hauled away
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);

            // Go to the food
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Stand there and stare
            Toil stare = Toils_General.Wait(waitTicks);
            stare.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
            };
            
            stare.socialMode = RandomSocialMode.Quiet;
            stare.defaultCompleteMode = ToilCompleteMode.Delay;

            yield return stare;
        }
    }
}