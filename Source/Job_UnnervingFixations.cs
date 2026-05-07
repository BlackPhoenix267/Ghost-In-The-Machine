using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace GITM
{
      public class JobGiver_UnnervingFixation : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;

            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || (comp.strongRelations == null && (comp.traits == null || !comp.traits.ContainsKey(TraitDefOf.Disturbing)))) return null;

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextUnnervingFixationsEligibleTick) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            // 5-day MTB half-life
            if (!Rand.MTBEventOccurs(5f, 60000f, 60f)) return null;

            bool isJealous = comp.traits != null && comp.traits.Any(kvp => kvp.Key.defName == "Jealous");

            Pawn chosenTarget = null;

            var potentialTargets = pawn.Map.mapPawns.FreeColonistsAndPrisonersSpawned;
            foreach (Pawn target in potentialTargets)
            {
                if (target == pawn) continue;

                bool isHated = comp.strongRelations.ContainsKey(target) && comp.strongRelations[target] <= -75;
                bool isRivalLover = false;

                // If the mech is Jealous, check if the target is a lover of the mech's former lover
                if (isJealous && comp.formerLovers != null)
                {
                    foreach (Pawn formerLover in comp.formerLovers)
                    {
                        if (target != formerLover && LovePartnerRelationUtility.LovePartnerRelationExists(target, formerLover))
                        {
                            isRivalLover = true;
                            break;
                        }
                    }
                }

                if (isHated || isRivalLover || (comp.traits!=null && comp.traits.ContainsKey(TraitDefOf.Disturbing)))
                {
                    chosenTarget = target;
                    break;
                }
            }

            if (chosenTarget == null) return null;

            comp.nextUnnervingFixationsEligibleTick = currentTick + 60000;

            if (!chosenTarget.Awake())
            {
                return JobMaker.MakeJob(GITM_DefOf.GITM_UnnervingPractice, chosenTarget);
            }
            else
            {
                return JobMaker.MakeJob(GITM_DefOf.GITM_UnnervingStalking, chosenTarget);
            }
        }
    }
    // --- 1. ASLEEP: Unnerving Practice ---
    public class JobDriver_UnnervingPractice : JobDriver
    {
        private const int WatchTicks = 5000;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            AddFailCondition(() =>
            {
                Pawn targetPawn = TargetA.Thing as Pawn;
                return targetPawn != null && targetPawn.Awake();
            });

            Toil findSpot = ToilMaker.MakeToil("FindPracticeSpot");
            findSpot.initAction = delegate
            {
                Pawn target = TargetA.Thing as Pawn;
                Room targetRoom = target.Position.GetRoom(Map);

                IntVec3 spot;
                bool found = CellFinder.TryFindRandomCellNear(target.Position, Map, 15, (IntVec3 c) =>
                {
                    if (!c.Standable(Map) || !pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly)) return false;
                    Room cellRoom = c.GetRoom(Map);
                    return cellRoom != null && cellRoom != targetRoom;
                }, out spot);

                if (!found) spot = CellFinder.RandomClosewalkCellNear(target.Position, Map, 4);
                job.targetB = spot;
            };
            yield return findSpot;

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            // "Pace" (Wander back and forth near the spot) while staying aware of the target
            Toil pace = Toils_General.Wait(WatchTicks);
            pace.tickAction = delegate
            {
                // Randomly fidget/rotate to simulate pacing and practicing
                if (Find.TickManager.TicksGame % 200 == 0)
                {
                    pawn.rotationTracker.FaceTarget(TargetA);
                }
            };

            pace.AddFinishAction(delegate
            {
                if (ticksLeftThisToil <= WatchTicks - 500)
                {
                    Pawn target = TargetA.Thing as Pawn;
                    if (target != null && target.needs?.mood != null)
                    {
                        target.needs.mood.thoughts.memories.TryGainMemory(GITM_DefOf.GITM_DisturbedSleepMalicious);
                    }
                }
            });

            yield return pace;
        }
    }

    // --- 2. AWAKE: Unnerving Stalking ---
    public class JobDriver_UnnervingStalking : JobDriver
    {
        private const int WatchTicks = 2500;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // Go right up to them
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil stare = Toils_General.Wait(WatchTicks);
            stare.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
            };

            // If the pawn walks away, the stalking ends early, but they still get the thought if it lasted long enough
            stare.FailOn(() => !pawn.Position.AdjacentTo8WayOrInside(TargetA.Thing.Position));

            stare.AddFinishAction(delegate
{
    if (ticksLeftThisToil <= WatchTicks - 500)
    {
        Pawn target = TargetA.Thing as Pawn;
        if (target != null && target.needs?.mood != null)
        {
            target.needs.mood.thoughts.memories.TryGainMemory(GITM_DefOf.GITM_UnnervingStalkingThought);
        }
    }
});

            yield return stare;
        }
    }
}