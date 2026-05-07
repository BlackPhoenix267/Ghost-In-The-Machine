using RimWorld;
using Verse;
using System.Collections.Generic;

namespace GITM
{
    public class ThoughtWorker_DisturbingMech : ThoughtWorker
    {
        private static TraitDef cachedDisturbingTrait;
        private static bool initialized = false;

        private static TraitDef DisturbingTrait
        {
            get
            {
                if (!initialized)
                {
                    cachedDisturbingTrait = DefDatabase<TraitDef>.GetNamedSilentFail("Disturbing");
                    initialized = true;
                }
                return cachedDisturbingTrait;
            }
        }

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (DisturbingTrait == null)
            {
                return ThoughtState.Inactive;
            }

            if (!p.Spawned || !p.RaceProps.Humanlike || p.Dead || !p.Awake())
            {
                return ThoughtState.Inactive;
            }

            float radiusSq = 100f;
            IReadOnlyList<Pawn> pawns = p.Map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];

                if (other != p && other.RaceProps.IsMechanoid && p.Position.DistanceToSquared(other.Position) < radiusSq)
                {
                    var comp = other.TryGetComp<CompMechSkillMemory>();

                    if (comp?.traits != null && comp.traits.ContainsKey(DisturbingTrait))
                    {
                        if (GenSight.LineOfSight(p.Position, other.Position, p.Map))
                        {
                            return ThoughtState.ActiveAtStage(0);
                        }
                    }
                }
            }

            return ThoughtState.Inactive;
        }
    }
}