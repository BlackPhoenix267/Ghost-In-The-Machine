using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GITM
{
    public class ScanState
    {
        public Dictionary<SkillDef, int> skills = new Dictionary<SkillDef, int>();
        public Dictionary<TraitDef, int> traits = new Dictionary<TraitDef, int>();
        public Dictionary<Pawn, int> strongRelations = new Dictionary<Pawn, int>();
        public List<Pawn> formerLovers = new List<Pawn>();

        public bool isHighTier = false;
        public string sourcePawnName = "Unknown";
        public string scanDate = "Unknown";
        public float brainDamagePercent = 0f;
    }

    [HarmonyPatch]
    public static class Patch_SubcoreScanner_Tick
    {
        private static readonly List<string> whitelistedTraits = new List<string> {
            "BodyPurist", "Jealous", "Pyromaniac", "Bloodlust", "Kind",
            "Nimble", "Brawler", "TooSmart", "FastLearner", "SlowLearner",
            "DrugDesire", "Industriousness", "SpeedOffset", "Neurotic",
            "ShootingAccuracy", "Disturbing", "TorturedArtist", "Cannibal",
            "NightOwl", "Gourmand", "Undergrounder", "PsychicSensitivity"
        };

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Building_SubcoreScanner), "Tick");

            var subclasses = GenTypes.AllTypes.Where(t => t.IsSubclassOf(typeof(Building_SubcoreScanner)));
            foreach (var type in subclasses)
            {
                var customTick = AccessTools.DeclaredMethod(type, "Tick");
                if (customTick != null)
                {
                    yield return customTick;
                }
            }
        }

        public static void Prefix(Building_SubcoreScanner __instance, ref int ___fabricationTicksLeft, out ScanState __state)
        {
            __state = null;
            try
            {
                if (__instance.State == SubcoreScannerState.Occupied && ___fabricationTicksLeft > 0 && ___fabricationTicksLeft <= 20)
                {
                    Pawn occupant = __instance.Occupant;

                    if (occupant != null && occupant.skills != null)
                    {
                        __state = new ScanState();
                        string outputDef = __instance.def.building.subcoreScannerOutputDef.defName;
                        __state.isHighTier = (outputDef == "SubcoreHigh" || outputDef.Contains("High"));
                        __state.sourcePawnName = occupant.Name != null ? occupant.Name.ToStringFull : occupant.LabelShort;

                        Vector2 location = __instance.Map != null ? Find.WorldGrid.LongLatOf(__instance.Map.Tile) : Vector2.zero;
                        __state.scanDate = GenDate.DateFullStringAt(GenTicks.TicksAbs, location);
                        BodyPartRecord brain = occupant.health.hediffSet.GetBrain();
                        if (brain != null)
                        {
                            float maxHealth = brain.def.GetMaxHealth(occupant);
                            float currentHealth = occupant.health.hediffSet.GetPartHealth(brain);
                            __state.brainDamagePercent = 1f - (currentHealth / maxHealth);

                            // Clamp it just to be safe
                            if (__state.brainDamagePercent < 0f) __state.brainDamagePercent = 0f;
                            if (__state.brainDamagePercent > 1f) __state.brainDamagePercent = 1f;
                        }

                        // Process Skills
                        foreach (SkillRecord skill in occupant.skills.skills)
                        {
                            int level = skill.Level;
                            if (!__state.isHighTier && level > GITM_Mod.settings.standardSkillCap)
                            {
                                level = GITM_Mod.settings.standardSkillCap;
                            }
                            __state.skills[skill.def] = level;
                        }

                        if (__state.isHighTier)
                        {
                            if (occupant.story != null && occupant.story.traits != null)
                            {
                                foreach (Trait trait in occupant.story.traits.allTraits)
                                {
                                    if (whitelistedTraits.Contains(trait.def.defName))
                                    {
                                        if (!(trait.def.defName == "DrugDesire" && (trait.Degree == 1 || trait.Degree == -1)))
                                            __state.traits[trait.def] = trait.Degree;
                                    }
                                }

                                if (occupant.ageTracker != null && occupant.ageTracker.AgeBiologicalYears < 13)
                                {
                                    TraitDef childTrait = DefDatabase<TraitDef>.GetNamedSilentFail("GITM_Child");
                                    if (childTrait != null) __state.traits[childTrait] = 0;
                                }
                            }

                            if (occupant.relations != null)
                            {
                                var possibleTargets = new List<Pawn>(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners);
                                possibleTargets.AddRange(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony);

                                foreach (Pawn target in possibleTargets)
                                {
                                    if (target == occupant) continue;

                                    int opinion = occupant.relations.OpinionOf(target);
                                    if (opinion >= 75 || opinion <= -75)
                                    {
                                        __state.strongRelations[target] = opinion;
                                    }
                                }

                                if (occupant.relations.DirectRelations != null)
                                {
                                    foreach (DirectPawnRelation rel in occupant.relations.DirectRelations)
                                    {
                                        if (LovePartnerRelationUtility.IsLovePartnerRelation(rel.def))
                                        {
                                            if (occupant.relations.OpinionOf(rel.otherPawn) >= 75)
                                            {
                                                __state.formerLovers.Add(rel.otherPawn);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GITM] Prefix failed: {ex}");
            }
        }

        public static void Postfix(Building_SubcoreScanner __instance, ScanState __state)
        {
            if (__state == null) return;

            Thing newlySpawnedSubcore = null;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(__instance.Position, 4f, true))
            {
                if (!cell.InBounds(__instance.Map)) continue;
                List<Thing> thingsInCell = cell.GetThingList(__instance.Map);
                foreach (Thing thing in thingsInCell)
                {
                    if (thing.def == __instance.def.building.subcoreScannerOutputDef)
                    {
                        var comp = thing.TryGetComp<CompSubcoreSkillMemory>();
                        if (comp != null && (comp.skillLevels == null || comp.skillLevels.Count == 0))
                        {
                            newlySpawnedSubcore = thing;
                            break;
                        }
                    }
                }
                if (newlySpawnedSubcore != null) break;
            }

            if (newlySpawnedSubcore != null)
            {
                var comp = newlySpawnedSubcore.TryGetComp<CompSubcoreSkillMemory>();
                comp.skillLevels = __state.skills;
                comp.traits = __state.traits;
                comp.strongRelations = __state.strongRelations;
                comp.formerLovers = __state.formerLovers;
                comp.isHighTierCore = __state.isHighTier;
                comp.sourcePawnName = __state.sourcePawnName;
                comp.scanDate = __state.scanDate;
                comp.brainDamagePercent = __state.brainDamagePercent;
            }
        }
    }
}