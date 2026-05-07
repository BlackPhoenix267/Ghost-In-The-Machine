using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq; // Required for .FirstOrDefault()
using UnityEngine; // Added for Mathf.Clamp

namespace GITM
{
    [HarmonyPatch(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn), new[] { typeof(Pawn), typeof(SkillDef), typeof(bool) })]
    public static class Patch_GenerateQualityCreatedByPawn
    {
        public static bool Prefix(Pawn pawn, SkillDef relevantSkill, bool consumeInspiration, ref QualityCategory __result)
        {
            // 1. FILTER: If it's not a mech, or if it doesn't have our comp, let Vanilla run normally.
            if (pawn == null || !pawn.RaceProps.IsMechanoid) return true;
            
            var comp = pawn.GetComp<CompMechSkillMemory>();
            if (comp == null || pawn.skills == null) return true;

            // 2. OVERRIDE: We have a GITM mech. Use their actual skill level.
            int relevantSkillLevel = pawn.skills.GetSkill(relevantSkill).Level;
            
            
            QualityCategory qualityCategory = QualityUtility.GenerateQualityCreatedByPawn(relevantSkillLevel, false);
            
            // Handle Ideology roles (e.g., Production Specialists)
            if (ModsConfig.IdeologyActive && pawn.Ideo != null)
            {
                Precept_Role role = pawn.Ideo.GetRole(pawn);
                if (role != null && role.def.roleEffects != null)
                {
                    RoleEffect roleEffect = role.def.roleEffects.FirstOrDefault((RoleEffect eff) => eff is RoleEffect_ProductionQualityOffset);
                    if (roleEffect != null)
                    {
                        int offset = ((RoleEffect_ProductionQualityOffset)roleEffect).offset;
                        int newQualityLevel = Mathf.Clamp((int)qualityCategory + offset, 0, 6); // 0 is Awful, 6 is Legendary
                        qualityCategory = (QualityCategory)newQualityLevel;
                    }
                }
            }
            
            // Assign our calculated quality to the result
            __result = qualityCategory;
            
            // Return false to skip the original vanilla method
            return false;
        }
    }
}