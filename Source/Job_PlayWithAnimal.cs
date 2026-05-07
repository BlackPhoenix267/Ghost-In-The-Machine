using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    // --- THE BRAIN ---
    public class JobGiver_PlayWithAnimal : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;
            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            
            if (comp == null || comp.traits == null || !comp.traits.ContainsKey(TraitDefOf.Kind)) return null;

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextPlayWithAnimalsEligibleTick) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            // MTB: 15 days
            if (!Rand.MTBEventOccurs(15f, 60000f, 60f)) return null;

            // Find a nuzzleable animal in the colony that is awake and reachable
            var validAnimals = pawn.Map.mapPawns.SpawnedColonyAnimals
                .Where(a => a.RaceProps.nuzzleMtbHours > 0 && 
                            a.Awake() && 
                            !a.Downed && 
                            pawn.CanReach(a, PathEndMode.Touch, Danger.Some))
                .ToList();

            if (validAnimals.Count == 0) return null;

            Pawn chosenAnimal = validAnimals.RandomElement();

            comp.nextPlayWithAnimalsEligibleTick = currentTick + 60000;

            return JobMaker.MakeJob(GITM_DefOf.GITM_PlayWithAnimal, chosenAnimal);
        }
    }

    // --- THE BODY ---
    public class JobDriver_PlayWithAnimal : JobDriver
    {
        private int playDuration;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref playDuration, "playDuration", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            // 2-3 in-game hours (5000 to 7500 ticks)
            if (playDuration == 0)
            {
                playDuration = Rand.Range(5000, 7500); 
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            
            // Fail if the animal goes to sleep or goes down 
            this.FailOn(() => {
                Pawn animal = TargetA.Thing as Pawn;
                return animal == null || animal.Downed || !animal.Awake();
            });

            // Initial goto
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Custom tracking toil
            Toil play = new Toil();
            play.defaultCompleteMode = ToilCompleteMode.Never;
            play.tickAction = delegate
            {
                Pawn actor = play.actor;
                Pawn animal = (Pawn)TargetA.Thing;

                // If the animal walked away, move closer
                if (!actor.Position.AdjacentTo8WayOrInside(animal.Position))
                {
                    if (!actor.pather.Moving)
                    {
                        actor.pather.StartPath(animal, PathEndMode.Touch);
                    }
                }
                else
                {
                    // We are close enough, stop walking and face it
                    if (actor.pather.Moving)
                    {
                        actor.pather.StopDead();
                    }
                    actor.rotationTracker.FaceTarget(TargetA);
                    
                    if (actor.IsHashIntervalTick(400))
                    {
                        FleckMaker.ThrowMetaIcon(actor.Position, actor.Map, FleckDefOf.Heart);
                    }
                }

                // Tick down the duration whether moving or standing still so they don't chase forever
                playDuration--;
                if (playDuration <= 0)
                {
                    actor.jobs.curDriver.ReadyForNextToil();
                }
            };
            
            yield return play;
        }
    }
}