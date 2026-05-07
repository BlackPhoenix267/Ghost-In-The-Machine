using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    public class JobGiver_ObserveCorpse : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;
            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || comp.traits == null || (!comp.traits.ContainsKey(TraitDefOf.Bloodlust) && !comp.traits.ContainsKey(GITM_DefOf.Cannibal))) return null;

            int currentTick = Find.TickManager.TicksGame;
            
            if (currentTick < comp.nextObserveCorpseEligibleTick) return null;

            Corpse targetCorpse = null;
            float closestDistSq = 999999f;

            foreach (Thing thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                Corpse corpse = thing as Corpse;
                if (corpse != null && corpse.GetRotStage() == RotStage.Fresh)
                {
                    // Ensure the mech can actually reach it
                    if (pawn.CanReach(corpse, PathEndMode.Touch, Danger.Some))
                    {
                        float distSq = pawn.Position.DistanceToSquared(corpse.Position);
                        if (distSq < closestDistSq)
                        {
                            closestDistSq = distSq;
                            targetCorpse = corpse;
                        }
                    }
                }
            }

            if (targetCorpse == null) return null;

            comp.nextObserveCorpseEligibleTick= currentTick + 420000;
            return JobMaker.MakeJob(GITM_DefOf.GITM_ObserveCorpse, targetCorpse);
        }
    }

    public class JobDriver_ObserveCorpse : JobDriver
    {
        private const int ObserveTicks = 5000;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserving the corpse prevents haulers from taking it while the mech is staring
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            // Abort the job if the corpse rots while the mech is walking to it or watching it
            this.FailOn(() => ((Corpse)TargetThingA).GetRotStage() != RotStage.Fresh);

            // Go to the corpse
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Stare at it
            Toil observe = Toils_General.Wait(ObserveTicks);
            observe.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
            };

            yield return observe;
        }
    }
}