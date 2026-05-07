using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GITM
{
    public class CompProperties_SubcoreSkillMemory : CompProperties
    {
        public CompProperties_SubcoreSkillMemory() => compClass = typeof(CompSubcoreSkillMemory);
    }

    public class CompSubcoreSkillMemory : ThingComp
    {
        public Dictionary<SkillDef, int> skillLevels = new Dictionary<SkillDef, int>();
        public Dictionary<TraitDef, int> traits = new Dictionary<TraitDef, int>();
        public Dictionary<Pawn, int> strongRelations = new Dictionary<Pawn, int>();
        public List<Pawn> formerLovers = new List<Pawn>();
        private List<Pawn> tmpRelKeys;
        private List<int> tmpRelVals;
        public bool isHighTierCore = false;

        public string sourcePawnName = "Unknown";
        public string scanDate = "Unknown";
        public float brainDamagePercent = 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref skillLevels, "skillLevels", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref traits, "traits", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref strongRelations, "strongRelations", LookMode.Reference, LookMode.Value, ref tmpRelKeys, ref tmpRelVals);
            Scribe_Collections.Look(ref formerLovers, "formerLovers", LookMode.Reference);

            Scribe_Values.Look(ref isHighTierCore, "isHighTierCore", false);
            Scribe_Values.Look(ref sourcePawnName, "sourcePawnName", "Unknown");
            Scribe_Values.Look(ref scanDate, "scanDate", "Unknown");
            Scribe_Values.Look(ref brainDamagePercent, "brainDamagePercent", 0f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (skillLevels == null) skillLevels = new Dictionary<SkillDef, int>();
                if (traits == null) traits = new Dictionary<TraitDef, int>();
                if (strongRelations == null) strongRelations = new Dictionary<Pawn, int>();
                if (formerLovers == null) formerLovers = new List<Pawn>();
                formerLovers.RemoveAll(x => x == null);

                var cleanedRelations = new Dictionary<Pawn, int>();
                foreach (var kvp in strongRelations)
                {
                    if (kvp.Key != null)
                    {
                        cleanedRelations[kvp.Key] = kvp.Value;
                    }
                }
                strongRelations = cleanedRelations;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (skillLevels == null || skillLevels.Count == 0) return "No brain data recorded.";

            string text = $"Brain Data ({(isHighTierCore ? "High" : "Standard")}):";
            if (sourcePawnName != "Unknown") text += $"\nSource: {sourcePawnName}";
            if (scanDate != "Unknown") text += $"\nScanned: {scanDate}";
            if (brainDamagePercent > 0f)
        {
            text += $"\nBrain Integrity Loss: {(brainDamagePercent * 100f):F0}%";
        }

            foreach (var kvp in skillLevels)
            {
                text += $"\n- {kvp.Key.LabelCap}: {kvp.Value}";
            }
            if (traits != null && traits.Count > 0)
            {
                text += "\nTraits:";
                foreach (var kvp in traits)
                {
                    text += $"\n- {kvp.Key.DataAtDegree(kvp.Value).label.CapitalizeFirst()}";
                }
            }
            return text;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            // Only show the gizmo if this core actually has brain data
            if (skillLevels == null || skillLevels.Count == 0) yield break;

            yield return new Command_Target
            {
                defaultLabel = "Replace Mech Core",
                defaultDesc = "Target a mechanoid to replace its current subcore with this one. If the mech has skills, its existing subcore will be ejected.",
                icon = parent.def.uiIcon,
                targetingParams = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetBuildings = false,
                    validator = (TargetInfo t) => t.Thing is Pawn p && p.RaceProps.IsMechanoid
                },
                action = delegate(LocalTargetInfo target)
                {
                    Pawn mech = target.Thing as Pawn;
                    CompMechSkillMemory mechComp = mech.GetComp<CompMechSkillMemory>();

                    // Check if the component exists and actually has old data to eject
                    if (mechComp != null && ((mechComp.skillLevels != null && mechComp.skillLevels.Count > 0) || mechComp.sourcePawnName != "Unknown"))
                    {
                        ThingDef oldCoreDef = mechComp.isHighTierCore ? DefDatabase<ThingDef>.GetNamed("SubcoreHigh") : DefDatabase<ThingDef>.GetNamed("SubcoreRegular");
                        Thing oldCore = ThingMaker.MakeThing(oldCoreDef);
                        CompSubcoreSkillMemory oldComp = oldCore.TryGetComp<CompSubcoreSkillMemory>();

                        if (oldComp != null)
                        {
                            oldComp.isHighTierCore = mechComp.isHighTierCore;
                            oldComp.sourcePawnName = mechComp.sourcePawnName;
                            oldComp.scanDate = mechComp.scanDate;
                            oldComp.brainDamagePercent = mechComp.brainDamagePercent;
                            
                            if (mechComp.skillLevels != null) oldComp.skillLevels = new Dictionary<SkillDef, int>(mechComp.skillLevels);
                            if (mechComp.traits != null) oldComp.traits = new Dictionary<TraitDef, int>(mechComp.traits);
                            if (mechComp.strongRelations != null) oldComp.strongRelations = new Dictionary<Pawn, int>(mechComp.strongRelations);
                            if (mechComp.formerLovers != null) oldComp.formerLovers = new List<Pawn>(mechComp.formerLovers);
                        }
                        GenPlace.TryPlaceThing(oldCore, mech.Position, mech.Map, ThingPlaceMode.Near);
                    }
                    else if (mechComp == null)
                    {
                        mechComp = new CompMechSkillMemory();
                        mechComp.parent = mech;
                        mech.AllComps.Add(mechComp);
                    }

                    mechComp.isHighTierCore = this.isHighTierCore;
                    mechComp.sourcePawnName = this.sourcePawnName;
                    mechComp.scanDate = this.scanDate;
                    mechComp.brainDamagePercent = this.brainDamagePercent;
                    
                    if (this.skillLevels != null) mechComp.skillLevels = new Dictionary<SkillDef, int>(this.skillLevels);
                    if (this.traits != null) mechComp.traits = new Dictionary<TraitDef, int>(this.traits);
                    if (this.strongRelations != null) mechComp.strongRelations = new Dictionary<Pawn, int>(this.strongRelations);
                    if (this.formerLovers != null) mechComp.formerLovers = new List<Pawn>(this.formerLovers);

                    mechComp.ApplyMemoryToVanillaTracker();

                    Messages.Message($"Successfully overwrote {mech.LabelShort}'s core with {this.sourcePawnName}'s scan data.", mech, MessageTypeDefOf.PositiveEvent);

                    parent.Destroy();
                }
            };
        }
    }
}