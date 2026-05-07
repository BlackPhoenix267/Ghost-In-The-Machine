using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GITM
{
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.AnyPlayerMechCanDoWork))]
    public static class MechanitorUtility_AnyPlayerMechCanDoWork_Patch
    {
        public static bool Prefix(WorkTypeDef workType, int skillRequired, out Pawn pawn, ref bool __result)
        {
            if (!ModsConfig.BiotechActive)
            {
                pawn = null;
                __result = false;
                return false; // Skip original
            }

            List<Pawn> list = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < list.Count; i++)
            {
                Pawn pawn2 = list[i];
                if (pawn2.IsColonyMech && pawn2.GetOverseer() != null && pawn2.RaceProps.mechEnabledWorkTypes.Contains(workType))
                {
                    // Default to the vanilla hardcoded level
                    int currentSkillLevel = pawn2.RaceProps.mechFixedSkillLevel;

                    // GITM OVERRIDE: If Comp gave this mech a skill tracker, calculate the skill dynamically
                    if (pawn2.skills != null && workType.relevantSkills != null && workType.relevantSkills.Count > 0)
                    {
                        currentSkillLevel = 0; // Reset so we can find the max of the relevant skills
                        for (int j = 0; j < workType.relevantSkills.Count; j++)
                        {
                            int level = pawn2.skills.GetSkill(workType.relevantSkills[j]).Level;
                            if (level > currentSkillLevel)
                            {
                                currentSkillLevel = level;
                            }
                        }
                    }

                    // Check if they pass the threshold
                    if (currentSkillLevel >= skillRequired)
                    {
                        pawn = pawn2;
                        __result = true;
                        return false; // Skip original
                    }
                }
            }
            
            // No capable mech found
            pawn = null;
            __result = false;
            return false; // Skip original
        }
    }
}