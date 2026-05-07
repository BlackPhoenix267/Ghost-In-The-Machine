using RimWorld;
using Verse;
using UnityEngine;
using System.Text;
using System.Collections.Generic;

namespace GITM
{
    public class ITab_MechSkills : ITab
    {
        public ITab_MechSkills()
        {
            size = new Vector2(250f, 550f);
            labelKey = "Skills";
        }

        public override bool IsVisible
        {
            get
            {
                Pawn pawn = SelPawn;
                return pawn != null && pawn.RaceProps.IsMechanoid && pawn.skills != null;
            }
        }

        protected override void FillTab()
        {
            Pawn pawn = SelPawn;
            if (pawn == null || pawn.skills == null) return;

            // Grab custom comp to access the traits
            CompMechSkillMemory comp = pawn.TryGetComp<CompMechSkillMemory>();

            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            GUI.BeginGroup(rect);

            // SKILLS
            Rect skillsRect = new Rect(0f, 0f, rect.width, 320f);
            SkillUI.DrawSkillsOf(pawn, new Vector2(0f, 0f), SkillUI.SkillDrawMode.Gameplay, skillsRect);

            // TRAITS
            if (comp != null && comp.traits != null && comp.traits.Count > 0)
            {
                float curY = 330f;

                // Header
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, curY, rect.width, 30f), "Retained Traits");
                Text.Font = GameFont.Small;
                curY += 30f;

                // Loop through and draw each trait
                foreach (var kvp in comp.traits)
                {
                    TraitDef def = kvp.Key;
                    int degree = kvp.Value;
                    TraitDegreeData traitData = def.DataAtDegree(degree);

                    if (traitData == null) continue;

                    Rect traitRect = new Rect(0f, curY, rect.width, 24f);

                    if (Mouse.IsOver(traitRect))
                    {
                        Widgets.DrawHighlight(traitRect);
                    }

                    // Draw the trait name
                    Widgets.Label(traitRect, traitData.label.CapitalizeFirst());

                    TooltipHandler.TipRegion(traitRect, () => GetTraitTooltip(def, degree, pawn), (int)(curY * 13.5f));

                    curY += 24f;
                }
            }

            GUI.EndGroup();
        }

        private string GetTraitTooltip(TraitDef def, int degree, Pawn pawn)
        {
            TraitDegreeData traitData = def.DataAtDegree(degree);
            StringBuilder sb = new StringBuilder();

            // Custom Description Logic
            string rawCustomDesc = null;

            switch (def.defName)
            {
                case "NightOwl":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone who far preferred being up during the night. This may affect its behavior.";
                    break;
                case "Undergrounder":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone who preferred being underground. This may affect its behavior.";
                    break;
                case "BodyPurist":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after a body purist. Whether due to incapability to fully grasp its condition or a mechanitor's psychic control, it is mostly able to work, but it will occasionally react in horror to its new condition.";
                    break;
                case "Gourmand":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone whose life revolved around food. This may affect its behavior.";
                    break;
                case "Jealous":
                    rawCustomDesc = "{PAWN_nameDef} has deeply jealous neural patterns. It will resent the new partners of its former lovers, though it's unclear how this resentment might manifest.";
                    break;
                case "Pyromaniac":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone who loved fire. This may affect its behavior.";
                    break;
                case "Bloodlust":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone who found great joy in the pain of others. This may affect its behavior.";
                    break;
                case "Cannibal":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone who loved consuming human flesh. This may affect its behavior.";
                    break;
                case "TorturedArtist":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone who valued art greatly. This may affect its behavior.";
                    break;
                case "Kind":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone exceptionally kind and compassionate. This may affect its behavior.";
                    break;
                case "ChemicalFascination":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone utterly fascinated by recreational drugs. This may affect its behavior.";
                    break;
                case "Neurotic":
                    rawCustomDesc = "{PAWN_nameDef} has neurotic neural patterns. They cause it to work faster, but also expend more energy on mundane tasks.\n\nGlobal work speed +20%\nEnergy consumption +40%";
                    break;
                case "VeryNeurotic":
                    rawCustomDesc = "{PAWN_nameDef} has deeply anxious and neurotic neural patterns. They cause it to work much faster, but also expend far more energy on mundane tasks.\n\nGlobal work speed +40%\nEnergy consumption +80%";
                    break;
                case "PsychicallyHypersensitive":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone extraordinarily attuned to psychic phenomena, and this hypersensitivity appears to have carried over.\nThis makes it much easier for mechanitors to control it through psychic means.\n\nPsychic sensitivity +80%\nMech bandwidth usage -50%";
                    break;
                case "PsychicallySensitive":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone unusually attuned to psychic phenomena, and this sensitivity appears to have carried over.\nThis makes it easier for mechanitors to control it through psychic means.\n\nPsychic sensitivity +40%\nMech bandwidth usage -25%";
                    break;
                case "PsychicallyDull":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone unusually out of tune with psychic phenomena, and this reduced sensitivity appears to have carried over.\nThis makes it harder for mechanitors to control it through psychic means.\n\nPsychic sensitivity -50%\nMech bandwidth usage +50%";
                    break;
                case "PsychicallyDeaf":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone totally disconnected from psychic phenomena, and this deafness appears to have carried over.\nHaving no psychic sensitivity makes it impossible for mechanitors to psychically control it, forcing them to do so entirely through radio transmissions.\n\nPsychic sensitivity -100%\nMech bandwidth usage +100%";
                    break;
                case "Disturbing":
                    rawCustomDesc = "{PAWN_nameDef}'s subcore was patterned after someone deeply disturbing. Though now eternally silent, it still behaves in a disturbing and unnerving way that others find upsetting.";
                    break;
            }

            if (rawCustomDesc != null)
            {
                string resolvedCustomDesc = rawCustomDesc.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                sb.AppendLine(resolvedCustomDesc);
            }
            else
            {
                string resolvedVanillaDesc = traitData.description.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                sb.AppendLine(resolvedVanillaDesc);
                bool hasStats = false;
                if (traitData.statOffsets != null)
                {
                    sb.AppendLine();
                    foreach (var mod in traitData.statOffsets)
                    {
                        sb.AppendLine($"{mod.stat.LabelCap}: {mod.ValueToStringAsOffset}");
                        hasStats = true;
                    }
                }

                if (traitData.statFactors != null)
                {
                    if (!hasStats) sb.AppendLine();
                    foreach (var mod in traitData.statFactors)
                    {
                        sb.AppendLine($"{mod.stat.LabelCap}: {mod.ToStringAsFactor}");
                    }
                }
            }

            return sb.ToString().TrimEndNewlines();
        }
    }

    [StaticConstructorOnStartup]
    public static class MechTabInjector
    {
        static MechTabInjector()
        {
            // Iterate through every ThingDef in the game
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                // Check if the ThingDef is a pawn and is classified as a mechanoid
                if (def.race != null && def.race.IsMechanoid)
                {
                    // Ensure the inspectorTabs list exists
                    if (def.inspectorTabs == null)
                    {
                        def.inspectorTabs = new List<System.Type>();
                    }

                    // Add our custom tab if it isn't already there
                    if (!def.inspectorTabs.Contains(typeof(ITab_MechSkills)))
                    {
                        def.inspectorTabs.Add(typeof(ITab_MechSkills));
                        
                        // Because StaticConstructorOnStartup runs AFTER defs are resolved,
                        // we must also add it to the resolved tabs list so it actually renders.
                        if (def.inspectorTabsResolved != null)
                        {
                            def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_MechSkills)));
                        }
                    }
                }
            }
        }
    }
}