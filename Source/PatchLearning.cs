using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace GITM
{
    // This patch intercepts the calculation that decides how fast a pawn learns a skill.
    [HarmonyPatch(typeof(SkillRecord), "LearnRateFactor")]
    public static class Patch_SkillRecord_LearnRateFactor
    {
        public static bool Prefix(SkillRecord __instance, ref float __result, Pawn ___pawn, bool direct = false)
        {
            if (___pawn == null || !___pawn.RaceProps.IsMechanoid)
            {
                return true; 
            }

            var comp = ___pawn.TryGetComp<CompMechSkillMemory>();
            
            if (comp == null || comp.skillLevels == null || comp.skillLevels.Count == 0)
            {
                __result = 0f;
                return false; 
            }

            float rate = comp.isHighTierCore ? GITM_Mod.settings.highLearningRate : GITM_Mod.settings.standardLearningRate;

            if (!direct)
            {
                // Traits like 'Fast Learner' are applied here
                rate *= ___pawn.GetStatValue(StatDefOf.GlobalLearningFactor);
            }

            __result = rate;
            return false; 
        }
    }

    [HarmonyPatch(typeof(SkillUI), "GetSkillDescription")]
    public static class Patch_SkillUI_GetSkillDescription
    {
        public static void Prefix(SkillRecord sk, out float __state)
        {
            __state = sk.xpSinceMidnight;

            // Hide the >4000f soft-cap warning
            if (sk.Pawn != null && sk.Pawn.RaceProps.IsMechanoid)
            {
                sk.xpSinceMidnight = 0f; 
            }
        }

        public static void Postfix(SkillRecord sk, float __state, ref string __result)
        {
            if (sk.Pawn != null && sk.Pawn.RaceProps.IsMechanoid)
            {
                // 1. Restore the true xpSinceMidnight value
                sk.xpSinceMidnight = __state;

                var comp = sk.Pawn.TryGetComp<CompMechSkillMemory>();
                if (comp == null || comp.skillLevels == null || comp.skillLevels.Count == 0)
                {
                    return;
                }

                // 2. Recreate vanilla's internal math so we can target the exact strings
                float statValue = sk.Pawn.GetStatValue(StatDefOf.GlobalLearningFactor);
                float animalFactor = 1f;
                float vanillaTotalRate = statValue * sk.passion.GetLearningFactor();
                
                if (sk.def == SkillDefOf.Animals)
                {
                    animalFactor = sk.Pawn.GetStatValue(StatDefOf.AnimalsLearningFactor);
                    vanillaTotalRate *= animalFactor;
                }

                // 3. Calculate mod's actual rates
                float actualTotalRate = sk.LearnRateFactor();
                float customBaseRate = comp.isHighTierCore ? GITM_Mod.settings.highLearningRate : GITM_Mod.settings.standardLearningRate;

                // 4. Construct the exact vanilla strings
                string vanillaTotalLine = ("LearningSpeed".Translate() + ": ").AsTipTitle() + vanillaTotalRate.ToStringPercent();
                string vanillaPassionLine = "  - " + sk.passion.GetLabel() + ": x" + sk.passion.GetLearningFactor().ToStringPercent("F0");
                
                // 5. Construct the custom replacement strings
                string replacementTotalLine = ("LearningSpeed".Translate() + ": ").AsTipTitle() + actualTotalRate.ToStringPercent();
                string replacementPassionLine = "  - " + "Subcore base rate" + ": x" + customBaseRate.ToStringPercent();

                // 6. Swap the text out
                if (__result.Contains(vanillaTotalLine))
                {
                    __result = __result.Replace(vanillaTotalLine, replacementTotalLine);
                }

                if (__result.Contains(vanillaPassionLine))
                {
                    __result = __result.Replace(vanillaPassionLine, replacementPassionLine);
                }
                else
                {
                    // Fallback catch just in case another mod wiped the passion line
                    __result += "\n\n  - Subcore learning rate: x" + actualTotalRate.ToStringPercent();
                }
            }
        }
    }
}