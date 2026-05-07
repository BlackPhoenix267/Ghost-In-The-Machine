using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace GITM
{
    // 1. Trigger Rescue when a loved one is downed
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_RescueTrigger
    {
        public static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn)
        {
            Pawn downedPawn = ___pawn;
            if (downedPawn == null || !downedPawn.RaceProps.Humanlike || downedPawn.Map == null) return;

            foreach (Pawn mech in downedPawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (!mech.RaceProps.IsMechanoid || mech.Downed || mech.Dead) continue;

                var comp = mech.TryGetComp<CompMechSkillMemory>();
                if (comp != null && ((comp.strongRelations.TryGetValue(downedPawn, out int opinion) && opinion >= 75) || (comp.traits!=null && comp.traits.ContainsKey(TraitDefOf.Kind))))
                {
                    bool uncontrolled = mech.GetOverseer() == null;
                    float chance = uncontrolled ? 1.0f : 0.10f;

                    if (Rand.Value < chance)
                    {
                        if (mech.mindState.mentalStateHandler.TryStartMentalState(GITM_DefOf.GITM_RescuingPawn, forceWake: true))
                        {
                            var state = mech.mindState.mentalStateHandler.CurState as MentalState_RescuingPawn;
                            if (state != null)
                            {
                                state.targetPawn = downedPawn;
                            }
                        }
                    }
                }
            }
        }
    }

    // 2. Trigger Leaving Peacefully / Murderous Rage when going feral
    [HarmonyPatch(typeof(CompOverseerSubject), "ForceFeral")]
    public static class Patch_FeralTrigger
    {
        public static void Postfix(CompOverseerSubject __instance)
        {
            if (__instance.parent is Pawn mech)
            {
                var comp = mech.TryGetComp<CompMechSkillMemory>();
                if (comp == null) return;

                if (comp.traits != null && comp.traits.ContainsKey(TraitDefOf.Kind))
                {
                    // Trigger the map exit mental state
                    mech.mindState.mentalStateHandler.TryStartMentalState(
                        GITM_DefOf.GITM_PeacefulDeparture, 
                        forceWake: true
                    );
                    
                    // Return early so a 'Kind' mech doesn't accidentally trigger Murderous Rage
                    return; 
                }

                // Trigger Murderous Rage for hated pawns when going feral
                if (comp.strongRelations != null)
                {
                    Pawn target = comp.strongRelations
                        .Where(kvp => kvp.Value <= -75 && kvp.Key.Spawned && kvp.Key.Map == mech.Map)
                        .Select(kvp => kvp.Key)
                        .RandomElementWithFallback();

                    if (target != null)
                    {
                        MentalStateDef rageDef = DefDatabase<MentalStateDef>.GetNamed("MurderousRage");
                        if (mech.mindState.mentalStateHandler.TryStartMentalState(rageDef, forceWake: true))
                        {
                            var state = mech.mindState.mentalStateHandler.CurState as MentalState_MurderousRage;
                            if (state != null)
                            {
                                state.target = target;
                            }
                        }
                    }
                }
            }
        }
    }
}