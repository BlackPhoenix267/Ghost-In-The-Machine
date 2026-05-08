using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace GITM
{
    public static class CorpseScanUtility
    {
        private static readonly List<string> whitelistedTraits = new List<string> {
            "BodyPurist", "Jealous", "Pyromaniac", "Bloodlust", "Kind",
            "Nimble", "Brawler", "TooSmart", "FastLearner", "SlowLearner",
            "DrugDesire", "Industriousness", "SpeedOffset", "Neurotic",
            "ShootingAccuracy", "Disturbing", "TorturedArtist", "Cannibal",
            "NightOwl", "Gourmand", "Undergrounder", "PsychicSensitivity"
        };

        // Calculates the combined brain damage from physical health and rot
        public static float CalculateCorpseBrainDamage(Corpse corpse, out BodyPartRecord brain)
        {
            Pawn pawn = corpse.InnerPawn;
            brain = pawn.health.hediffSet.GetBrain();
            if (brain == null) return 1f; // 100% damaged if brain is missing

            // 1. Calculate physical integrity
            float maxHealth = brain.def.GetMaxHealth(pawn);
            float currentHealth = pawn.health.hediffSet.GetPartHealth(brain);
            float physicalIntegrity = currentHealth / maxHealth;

            // 2. Calculate rot-based integrity loss
            float rotFraction = 0f;
            CompRottable compRot = corpse.GetComp<CompRottable>();
            if (compRot != null && compRot.PropsRot != null)
            {
                // TicksToDessicated is the point where the corpse becomes a skeleton (brain is entirely gone conceptually)
                float maxRotTicks = compRot.PropsRot.TicksToDessicated;
                rotFraction = Mathf.Clamp01(compRot.RotProgress / maxRotTicks);
            }

            // Lerp between initial and final settings based on how far along the rot progress is
            float rotIntegrity = Mathf.Lerp(GITM_Mod.settings.initialCorpseBrainIntegrity, GITM_Mod.settings.finalCorpseBrainIntegrity, rotFraction);

            // 3. Combine them. If either is 0, integrity is 0.
            float totalIntegrity = physicalIntegrity * rotIntegrity;
            return Mathf.Clamp01(1f - totalIntegrity); // Return as damage percentage
        }

        // Shared logic to extract the data into the ScanState for both living and dead pawns
        public static ScanState ExtractScanData(Pawn occupant, bool isHighTier, float brainDamagePercent)
        {
            ScanState state = new ScanState
            {
                isHighTier = isHighTier,
                sourcePawnName = occupant.Name != null ? occupant.Name.ToStringFull : occupant.LabelShort,
                brainDamagePercent = brainDamagePercent
            };

            Vector2 location = occupant.Map != null ? Find.WorldGrid.LongLatOf(occupant.Map.Tile) : Vector2.zero;
            state.scanDate = GenDate.DateFullStringAt(GenTicks.TicksAbs, location);

            if (occupant.skills != null)
            {
                foreach (SkillRecord skill in occupant.skills.skills)
                {
                    int level = skill.Level;
                    if (!isHighTier && level > GITM_Mod.settings.standardSkillCap)
                    {
                        level = GITM_Mod.settings.standardSkillCap;
                    }
                    state.skills[skill.def] = level;
                }
            }

            if (isHighTier)
            {
                if (occupant.story != null && occupant.story.traits != null)
                {
                    foreach (Trait trait in occupant.story.traits.allTraits)
                    {
                        if (whitelistedTraits.Contains(trait.def.defName))
                        {
                            if (!(trait.def.defName == "DrugDesire" && (trait.Degree == 1 || trait.Degree == -1)))
                                state.traits[trait.def] = trait.Degree;
                        }
                    }

                    if (occupant.ageTracker != null && occupant.ageTracker.AgeBiologicalYears < 13)
                    {
                        TraitDef childTrait = DefDatabase<TraitDef>.GetNamedSilentFail("GITM_Child");
                        if (childTrait != null) state.traits[childTrait] = 0;
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
                            state.strongRelations[target] = opinion;
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
                                    state.formerLovers.Add(rel.otherPawn);
                                }
                            }
                        }
                    }
                }
            }

            return state;
        }

        // Executes the manual corpse scan
        public static void PerformCorpseScan(Building_SubcoreScanner scanner, Corpse corpse)
        {
            float brainDamage = CalculateCorpseBrainDamage(corpse, out BodyPartRecord brain);
            
            // 1. Generate Subcore Data
            bool isHighTier = scanner.def.building.subcoreScannerOutputDef.defName.Contains("High");
            ScanState state = ExtractScanData(corpse.InnerPawn, isHighTier, brainDamage);

            // 2. Destroy the Corpse's brain
            if (brain != null)
            {
                int damageAmount = 9999; // Ensure total destruction
                DamageInfo dinfo = new DamageInfo(DamageDefOf.SurgicalCut, damageAmount, 999f, -1f, null, brain, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true);
                corpse.InnerPawn.TakeDamage(dinfo);
            }

            // 3. Consume scanner ingredients (if any are loaded/required)
            
            // If the building uses CompIngredients to track what was put in, clear the underlying list:
            var compIngredients = scanner.GetComp<CompIngredients>();
            if (compIngredients != null)
            {
                compIngredients.ingredients.Clear();
            }

            // CRITICAL: To actually consume the physical materials (Steel, Components, etc.) 
            // loaded into the scanner so the player doesn't get them back:
            scanner.GetDirectlyHeldThings()?.ClearAndDestroyContents();
            
            // 4. Spawn the subcore
            Thing subcore = ThingMaker.MakeThing(scanner.def.building.subcoreScannerOutputDef);
            var comp = subcore.TryGetComp<CompSubcoreSkillMemory>();
            if (comp != null)
            {
                comp.skillLevels = state.skills;
                comp.traits = state.traits;
                comp.strongRelations = state.strongRelations;
                comp.formerLovers = state.formerLovers;
                comp.isHighTierCore = state.isHighTier;
                comp.sourcePawnName = state.sourcePawnName;
                comp.scanDate = state.scanDate;
                comp.brainDamagePercent = state.brainDamagePercent;
            }

            GenPlace.TryPlaceThing(subcore, scanner.InteractionCell, scanner.Map, ThingPlaceMode.Near);
            Messages.Message($"Successfully ripped a subcore from the corpse of {state.sourcePawnName}.", subcore, MessageTypeDefOf.PositiveEvent);
        }
    }
}