using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    public class JobGiver_HuntSmallAnimal : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Drafted) return null;

            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null || comp.traits == null || !comp.traits.ContainsKey(TraitDefOf.Bloodlust)) return null;
            
            int currentTick = Find.TickManager.TicksGame;

            if (currentTick < comp.nextHuntSmallAnimalEligibleTick) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            if (!Rand.MTBEventOccurs(5f, 60000f, 60f)) return null;

            Pawn targetAnimal = null;
            float maxBodySize = pawn.BodySize * 0.25f;
            float searchRadiusSq = 20f * 20f; // 20 tiles squared, farther animals will not be chosen
            float closestDistSq = searchRadiusSq;

            // Find an animal
            foreach (Pawn potentialTarget in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (potentialTarget.RaceProps.Animal && !potentialTarget.Dead && !potentialTarget.Downed)
                {
                    // Check size constraint
                    if (potentialTarget.BodySize <= maxBodySize)
                    {
                        float distSq = pawn.Position.DistanceToSquared(potentialTarget.Position);
                        // Check distance and reachability
                        if (distSq <= closestDistSq && pawn.CanReach(potentialTarget, PathEndMode.Touch, Danger.Some))
                        {
                            closestDistSq = distSq;
                            targetAnimal = potentialTarget;
                        }
                    }
                }
            }

            if (targetAnimal == null) return null;

            comp.nextHuntSmallAnimalEligibleTick= currentTick + 60000;

            return JobMaker.MakeJob(JobDefOf.AttackMelee, targetAnimal);
        }
    }
}