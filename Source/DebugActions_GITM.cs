using Verse;
using RimWorld;
using LudeonTK;
using System.Collections.Generic;

namespace GITM
{
    public static class DebugActions_GITM
    {
        [DebugAction("Ghost in the Machine", "Spawn Regular Subcore (Scan)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnRegularSubcore(Pawn p)
        {
            GenerateCoreForPawn(p, false);
        }

        [DebugAction("Ghost in the Machine", "Spawn High Subcore (Scan)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnHighSubcore(Pawn p)
        {
            GenerateCoreForPawn(p, true);
        }

        private static void GenerateCoreForPawn(Pawn p, bool isHigh)
        {
            if (p == null || p.skills == null) return;

            ThingDef def = isHigh ? DefDatabase<ThingDef>.GetNamed("SubcoreHigh") : DefDatabase<ThingDef>.GetNamed("SubcoreRegular");
            Thing core = ThingMaker.MakeThing(def);
            CompSubcoreSkillMemory comp = core.TryGetComp<CompSubcoreSkillMemory>();

            if (comp != null)
            {
                comp.isHighTierCore = isHigh;
                comp.sourcePawnName = p.Name.ToStringShort;

                // FIXED: Converted Tile ID to Vector2 LongLat, and cast TicksAbs to long
                comp.scanDate = GenDate.DateFullStringAt((long)GenTicks.TicksAbs, Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile));
                BodyPartRecord brain = p.health.hediffSet.GetBrain();
                if (brain != null)
                {
                    float maxHealth = brain.def.GetMaxHealth(p);
                    float currentHealth = p.health.hediffSet.GetPartHealth(brain);
                    comp.brainDamagePercent = 1f - (currentHealth / maxHealth);

                    // Clamp it just to be safe
                    if (comp.brainDamagePercent < 0f) comp.brainDamagePercent = 0f;
                    if (comp.brainDamagePercent > 1f) comp.brainDamagePercent = 1f;
                }


                // Copy Skills
                foreach (SkillRecord skill in p.skills.skills)
                {
                    comp.skillLevels[skill.def] = skill.Level;
                }

                // Copy Traits and Relations (High Tier Only)
                if (isHigh)
                {
                    if (p.story != null && p.story.traits != null)
                    {
                        foreach (Trait trait in p.story.traits.allTraits)
                        {
                            comp.traits[trait.def] = trait.Degree;
                        }
                    }

                    if (p.relations != null)
                    {
                        // FIXED: Replaced DirectRelation with DirectPawnRelation
                        foreach (DirectPawnRelation rel in p.relations.DirectRelations)
                        {
                            if (rel.def == PawnRelationDefOf.Lover || rel.def == PawnRelationDefOf.Fiance || rel.def == PawnRelationDefOf.Spouse)
                            {
                                if (!comp.formerLovers.Contains(rel.otherPawn))
                                    comp.formerLovers.Add(rel.otherPawn);
                            }

                            int opinion = p.relations.OpinionOf(rel.otherPawn);
                            if (opinion > 75)
                            {
                                comp.strongRelations[rel.otherPawn] = opinion;
                            }
                        }
                    }
                }
            }

            GenPlace.TryPlaceThing(core, p.Position, p.Map, ThingPlaceMode.Near);
            Messages.Message($"Spawned {(isHigh ? "High" : "Regular")} GITM core for {p.LabelShort}.", core, MessageTypeDefOf.TaskCompletion);
        }
    }
}