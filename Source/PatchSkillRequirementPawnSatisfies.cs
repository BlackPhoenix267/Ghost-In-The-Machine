using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace GITM.Patches
{
    [HarmonyPatch(typeof(SkillRequirement), nameof(SkillRequirement.PawnSatisfies))]
    public static class SkillRequirement_PawnSatisfies_Patch
    {
        public static bool Prefix(SkillRequirement __instance, Pawn pawn, ref bool __result)
        {
            if (pawn.RaceProps.IsMechanoid)
            {
                var comp = pawn.GetComp<CompMechSkillMemory>();

                // If the mech has our comp, and it has actually loaded human skills...
                if (comp != null && comp.skillLevels != null && comp.skillLevels.Count > 0 && pawn.skills != null)
                {
                    bool chassisAllowsSkill = pawn.RaceProps.mechEnabledWorkTypes.Any(w => w.relevantSkills.NotNullAndContains(__instance.skill));

                    if (chassisAllowsSkill)
                    {
                        // Override the result with our tracked skill instead of mechFixedSkillLevel
                        __result = pawn.skills.GetSkill(__instance.skill).Level >= __instance.minLevel;
                    }
                    else
                    {
                        __result = false;
                    }

                    // Return false to skip the vanilla method entirely so it doesn't use the hardcoded fixed skill
                    return false;
                }
            }

            // If it's a human, or a standard un-upgraded mech, let vanilla handle it
            return true;
        }
    }
}