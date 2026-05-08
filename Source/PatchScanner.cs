using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
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

    // --- PATCH 1: LIVING PAWNS ---
    [HarmonyPatch]
    public static class Patch_SubcoreScanner_Tick
    {
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
                        string outputDef = __instance.def.building.subcoreScannerOutputDef.defName;
                        bool isHighTier = (outputDef == "SubcoreHigh" || outputDef.Contains("High"));

                        float brainDamagePercent = 0f;
                        BodyPartRecord brain = occupant.health.hediffSet.GetBrain();
                        if (brain != null)
                        {
                            float maxHealth = brain.def.GetMaxHealth(occupant);
                            float currentHealth = occupant.health.hediffSet.GetPartHealth(brain);
                            brainDamagePercent = Mathf.Clamp01(1f - (currentHealth / maxHealth));
                        }

                        // Use the shared extraction utility
                        __state = CorpseScanUtility.ExtractScanData(occupant, isHighTier, brainDamagePercent);
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

    // --- PATCH 2: CORPSE SCAN GIZMO ---
    [HarmonyPatch(typeof(Building_SubcoreScanner), "GetGizmos")]
    public static class Patch_SubcoreScanner_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Building_SubcoreScanner __instance)
        {
            // Yield vanilla gizmos
            foreach (var v in values)
            {
                yield return v;
            }

            if (__instance.def.defName == "SubcoreRipscanner" && __instance.State == SubcoreScannerState.WaitingForOccupant)
            {
                yield return new Command_Target
                {
                    defaultLabel = "Ripscan Corpse",
                    defaultDesc = "Extract a high subcore from an eligible colonist corpse. Factors in rot and existing damage. Destroys the brain.",
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/ExtractSkull", true),
                    targetingParams = new TargetingParameters
                    {
                        canTargetItems = true,
                        mapObjectTargetsMustBeAutoAttackable = false,
                        validator = (TargetInfo t) =>
                        {
                            if (t.Thing is Corpse corpse)
                            {
                                Pawn p = corpse.InnerPawn;
                                // Must be a colonist, must have a brain, must not be buried (implied by targeting)
                                if (p != null && p.IsColonist)
                                {
                                    float dmg = CorpseScanUtility.CalculateCorpseBrainDamage(corpse, out BodyPartRecord brain);
                                    // Valid if brain exists and damage is less than 100%
                                    return brain != null && dmg < 1f;
                                }
                            }
                            return false;
                        }
                    },
                    action = delegate (LocalTargetInfo target)
                    {
                        Corpse corpse = target.Thing as Corpse;
                        if (corpse != null)
                        {
                            // Find the closest capable and available colonist
                            Pawn hauler = __instance.Map.mapPawns.FreeColonistsSpawned
                                .Where(p => !p.Downed && !p.Dead &&
                                            p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                                            p.CanReserveAndReach(corpse, PathEndMode.Touch, Danger.Deadly) &&
                                            p.CanReserveAndReach(__instance, PathEndMode.Touch, Danger.Deadly))
                                .OrderBy(p => p.Position.DistanceToSquared(corpse.Position))
                                .FirstOrDefault();

                            if (hauler != null)
                            {
                                JobDef jobDef = DefDatabase<JobDef>.GetNamed("GITM_CarryCorpseToRipscanner");
                                Job job = JobMaker.MakeJob(jobDef, corpse, __instance);
                                job.count = 1;
                                hauler.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                                Messages.Message($"{hauler.LabelShort} is hauling {corpse.InnerPawn.LabelShort}'s body to the ripscanner.", hauler, MessageTypeDefOf.NeutralEvent);
                            }
                            else
                            {
                                Messages.Message("No available colonists to haul the corpse.", MessageTypeDefOf.RejectInput);
                            }
                        }
                    }
                };
            }
        }
    }
}