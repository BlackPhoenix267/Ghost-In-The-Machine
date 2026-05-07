using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;

namespace GITM.Patches
{
    [HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell))]
    public static class WorkGiver_Grower_JobOnCell_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var targetProperty = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonyMech));
            var replacementMethod = AccessTools.Method(typeof(WorkGiver_Grower_JobOnCell_Patch), nameof(IsColonyMechWithoutSkills));

            bool patched = false;

            foreach (var instruction in instructions)
            {
                if (!patched && instruction.Calls(targetProperty))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacementMethod);
                    patched = true;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (!patched)
            {
                Log.Error("[Ghost In The Machine] Transpiler failed: Could not find pawn.IsColonyMech in JobOnCell.");
            }
        }

        // By returning false if the mech has skills, the vanilla code 
        // skips the 'mechFixedSkillLevel' check and relies solely on pawn.skills
        public static bool IsColonyMechWithoutSkills(Pawn pawn)
        {
            return pawn.IsColonyMech && pawn.skills == null;
        }
    }
}