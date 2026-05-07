using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace GITM
{
    public class JobGiver_CheckHatedMurderousRage : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            // Only uncontrolled mechs
            if (pawn.GetOverseer() != null) return null;
            var comp = pawn.TryGetComp<CompMechSkillMemory>();
            if (comp == null) return null;

            if (!pawn.IsHashIntervalTick(60)) return null;

            // MTB 0.7 days (Extreme Break threshold)
            if (Rand.MTBEventOccurs(0.7f, 60000f, 60f))
            {
                Pawn target = comp.strongRelations
                    .Where(kvp => kvp.Value <= -75 && kvp.Key.Spawned && kvp.Key.Map == pawn.Map)
                    .Select(kvp => kvp.Key)
                    .RandomElementWithFallback();

                if (target != null)
                {
                    MentalStateDef rageDef = DefDatabase<MentalStateDef>.GetNamed("MurderousRage");

                    if (pawn.mindState.mentalStateHandler.TryStartMentalState(rageDef, forceWake: true))
                    {
                        var state = pawn.mindState.mentalStateHandler.CurState as MentalState_MurderousRage;
                        if (state != null)
                        {
                            state.target = target;
                        }
                    }
                }
            }
            return null;
        }
    }
}