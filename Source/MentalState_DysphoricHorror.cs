using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace GITM
{
    public class MentalState_DysphoricHorror : MentalState
    {
    }

    // The JobGiver that runs on loop while the state is active
    public class JobGiver_DysphoricHorror : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            int choice = Rand.RangeInclusive(0, 2);

            if (choice == 0)
            {
                // Scrape a nearby wall
                IntVec3 wallCell = IntVec3.Invalid;
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, 8f, true))
                {
                    if (cell.InBounds(pawn.Map))
                    {
                        Building edifice = cell.GetEdifice(pawn.Map);
                        if (edifice != null && edifice.def.holdsRoof) 
                        {
                            wallCell = cell;
                            break;
                        }
                    }
                }

                if (wallCell.IsValid)
                {
                    Job job = JobMaker.MakeJob(GITM_DefOf.GITM_ScrapeWall, wallCell);
                    job.expiryInterval = Rand.RangeInclusive(300, 600); // Drives the JobDriver duration
                    return job;
                }
            }
            else if (choice == 1)
            {
                // Try to wash it away
                IntVec3 waterCell = IntVec3.Invalid;
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, 20f, true))
                {
                    if (cell.InBounds(pawn.Map) && cell.GetTerrain(pawn.Map).IsWater)
                    {
                        waterCell = cell;
                        break;
                    }
                }

                if (waterCell.IsValid && pawn.CanReach(waterCell, PathEndMode.OnCell, Danger.Some))
                {
                    if (pawn.Position != waterCell)
                    {
                        Job gotoJob = JobMaker.MakeJob(GITM_DefOf.GITM_ErraticPacing, waterCell);
                        gotoJob.locomotionUrgency = LocomotionUrgency.Jog;
                        return gotoJob;
                    }
                    else
                    {
                        Job washJob = JobMaker.MakeJob(GITM_DefOf.GITM_WashAway);
                        washJob.expiryInterval = Rand.RangeInclusive(300, 600); 
                        return washJob;
                    }
                }
            }

            // Fallback: Erratic Pacing
            IntVec3 paceCell = RCellFinder.RandomWanderDestFor(pawn, pawn.Position, 12f, null, Danger.Some);
            if (paceCell.IsValid)
            {
                Job paceJob = JobMaker.MakeJob(GITM_DefOf.GITM_ErraticPacing, paceCell);
                paceJob.locomotionUrgency = LocomotionUrgency.Jog;
                paceJob.expiryInterval = 500;
                return paceJob;
            }

            return null;
        }
    }
}