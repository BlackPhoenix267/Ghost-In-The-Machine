using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace GITM.Patches
{
    [HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell))]
    public static class WorkGiver_GrowerSow_JobOnCell_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getRaceProps = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.RaceProps));
            
            var mechFixedSkillLevelField = AccessTools.Field(typeof(RaceProperties), nameof(RaceProperties.mechFixedSkillLevel));

            var helperMethod = AccessTools.Method(typeof(WorkGiver_GrowerSow_JobOnCell_Patch), nameof(GetEffectivePlantSkill));

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                // Look for a call to get_RaceProps followed by loading the mechFixedSkillLevel field
                if (i < codes.Count - 1 &&
                    codes[i].Calls(getRaceProps) &&
                    codes[i + 1].LoadsField(mechFixedSkillLevelField))
                {
                    // At this point in the IL stack, the 'Pawn' object is loaded.
                    // We change the get_RaceProps call to our helper method, which consumes the Pawn and returns an int.
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = helperMethod;

                    // Since our helper method already returns the final int, we delete (Nop) the vanilla fixed skill check.
                    codes[i + 1].opcode = OpCodes.Nop;
                    codes[i + 1].operand = null;

                    break; // We found and patched our target, no need to keep looping
                }
            }

            return codes;
        }

        // This replaces the result of `pawn.RaceProps.mechFixedSkillLevel` dynamically
        public static int GetEffectivePlantSkill(Pawn pawn)
        {
            // If it's a GITM mech (or a human), use their real skill
            if (pawn.skills != null)
            {
                return pawn.skills.GetSkill(SkillDefOf.Plants).Level;
            }

            // Fallback for vanilla mechs that don't have skills
            return pawn.RaceProps.mechFixedSkillLevel;
        }
    }
}