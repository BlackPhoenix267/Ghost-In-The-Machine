using System; // <-- Make sure this is here!
using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GITM
{
    [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanConstruct), new Type[] { typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef) })]
    public static class GenConstruct_CanConstruct_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Find the getter method for the Pawn.IsColonyMech property
            var isColonyMechGetter = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonyMech));
            
            // Custom replacement method
            var helperMethod = AccessTools.Method(typeof(GenConstruct_CanConstruct_Patch), nameof(ShouldDoVanillaMechSkillCheck));

            foreach (var instruction in instructions)
            {
                // When the game tries to check if it's a colony mech...
                if (instruction.Calls(isColonyMechGetter))
                {
                    // ...that instruction is replaced with a custom check
                    yield return new CodeInstruction(OpCodes.Call, helperMethod);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        // This method replaces `p.IsColonyMech` inside CanConstruct.
        public static bool ShouldDoVanillaMechSkillCheck(Pawn p)
        {
            // If the pawn is a colony mech BUT has your custom skill memory, 
            // return false so vanilla completely skips the `p.RaceProps.mechFixedSkillLevel` block.
            if (p.IsColonyMech && p.GetComp<CompMechSkillMemory>() != null)
            {
                return false; 
            }

            // Otherwise, behave exactly like vanilla
            return p.IsColonyMech;
        }
    }
}