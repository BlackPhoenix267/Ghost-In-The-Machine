using HarmonyLib;
using RimWorld;
using Verse;

namespace GITM
{
    [HarmonyPatch(typeof(Pawn_SkillTracker), "SkillsTickInterval")]
    public static class Patch_Pawn_SkillTracker_SkillsTickInterval
    {
        public static bool Prefix(Pawn ___pawn)
        {
            if (___pawn.RaceProps.IsMechanoid || ___pawn.story == null)
            {
                return false; 
            }
            return true; 
        }
    }
}