using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace GITM
{
    [HarmonyPatch(typeof(Building_MechGestator), "Notify_StartForming")]
    public static class Patch_MechGestator_TransferSkills
    {
        public static void Prefix(Building_MechGestator __instance)
        {
            var gestatorComp = __instance.TryGetComp<CompGestatorSkillMemory>();
            if (gestatorComp == null) return;

            foreach (Thing item in __instance.innerContainer)
            {
                var memoryComp = item.TryGetComp<CompSubcoreSkillMemory>();
                if (memoryComp != null && memoryComp.skillLevels != null && memoryComp.skillLevels.Count > 0)
                {
                    gestatorComp.storedSkills = new Dictionary<SkillDef, int>(memoryComp.skillLevels);
                    gestatorComp.storedTraits = new Dictionary<TraitDef, int>(memoryComp.traits);
                    gestatorComp.strongRelations = new Dictionary<Pawn, int>(memoryComp.strongRelations);
                    gestatorComp.formerLovers = new List<Pawn>(memoryComp.formerLovers);
                    
                    gestatorComp.isHighTierCore = memoryComp.isHighTierCore;
                    
                    gestatorComp.sourcePawnName = memoryComp.sourcePawnName;
                    gestatorComp.scanDate = memoryComp.scanDate;
                    gestatorComp.brainDamagePercent = memoryComp.brainDamagePercent;
                    
                    break; 
                }
            }
        }
    }
}