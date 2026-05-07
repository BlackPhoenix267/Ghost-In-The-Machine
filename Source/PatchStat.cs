using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic; // Required for HashSet

namespace GITM
{
    [HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
    public static class Patch_StatWorker_GetValueUnfinalized
    {
        // Categorize mechs for default values (enemy spawns or basic mechs without brain scans)
        private static readonly HashSet<string> BasicMechs = new HashSet<string>
        {
            "Mech_Militor", "Mech_Lifter", "Mech_Constructoid", "Mech_Cleansweeper", "Mech_Agrihand"
        };

        private static readonly HashSet<string> HighMechs = new HashSet<string>
        {
            "Mech_Legionary", "Mech_Tesseron", "Mech_Fabricor", "Mech_Paramedic", 
            "Mech_CentipedeBlaster", "Mech_CentipedeBurner", "Mech_CentipedeGunner"
        };

        public static void Postfix(StatWorker __instance, StatRequest req, bool applyPostProcess, ref float __result, StatDef ___stat)
        {
            if (req.HasThing && req.Thing is Pawn pawn && pawn.RaceProps.IsMechanoid)
            {
                var comp = pawn.TryGetComp<CompMechSkillMemory>();

                // BASE PSYCHIC SENSITIVITY OVERRIDES
                if (___stat.defName == "PsychicSensitivity")
                {
                    if (pawn.def.defName == "Mech_Apocriton")
                    {
                        __result = 1.0f;
                    }
                    else if (comp != null && comp.sourcePawnName != "Unknown")
                    {
                        // Player-crafted mechs that have an active brain scan memory
                        __result = comp.isHighTierCore ? 0.75f : 0.5f;
                    }
                    else
                    {
                        // Default fallback for enemy mechs or Basic mechs (which don't get brain scans)
                        if (BasicMechs.Contains(pawn.def.defName))
                        {
                            __result = 0f;
                        }
                        else if (HighMechs.Contains(pawn.def.defName))
                        {
                            __result = 0.75f;
                        }
                        else
                        {
                            // Standard mechs (Scyther, Lancer, Pikeman, etc.) default to 0.5
                            __result = 0.5f; 
                        }
                    }
                }

                // APPLY TRAIT OFFSETS & FACTORS
                if (comp != null && comp.traits != null && comp.traits.Count > 0)
                {
                    foreach (var kvp in comp.traits)
                    {
                        TraitDef def = kvp.Key;
                        int degree = kvp.Value;
                        TraitDegreeData traitData = def.DataAtDegree(degree);

                        if (traitData == null) continue;

                        // Apply Vanilla Flat Stat Additions (This handles Work Speed, Psychic Sensitivity offsets, etc.)
                        if (traitData.statOffsets != null)
                        {
                            foreach (StatModifier mod in traitData.statOffsets)
                            {
                                if (mod.stat == ___stat)
                                {
                                    __result += mod.value;
                                }
                            }
                        }

                        // Apply Vanilla Stat Multipliers
                        if (traitData.statFactors != null)
                        {
                            foreach (StatModifier mod in traitData.statFactors)
                            {
                                if (mod.stat == ___stat)
                                {
                                    __result *= mod.value;
                                }
                            }
                        }

                        // CUSTOM MECH TRAIT LOGIC

                        // Neurotic / Very Neurotic Energy Drain Penalty
                        if (___stat.defName == "MechEnergyUsageFactor")
                        {
                            if (def.defName == "Neurotic")
                            {
                                if (degree == 1) // Neurotic
                                {
                                    __result *= 1.40f; // 40% faster energy drain
                                }
                                else if (degree == 2) // Very Neurotic
                                {
                                    __result *= 1.80f; // 80% faster energy drain
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}