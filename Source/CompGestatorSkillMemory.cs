using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GITM
{
    public class CompProperties_GestatorSkillMemory : CompProperties
    {
        public CompProperties_GestatorSkillMemory() => this.compClass = typeof(CompGestatorSkillMemory);
    }

    public class CompGestatorSkillMemory : ThingComp
    {
        public Dictionary<SkillDef, int> storedSkills = new Dictionary<SkillDef, int>();
        public Dictionary<TraitDef, int> storedTraits = new Dictionary<TraitDef, int>();
        public Dictionary<Pawn, int> strongRelations = new Dictionary<Pawn, int>();
        public List<Pawn> formerLovers = new List<Pawn>();
        // Required temporary lists for saving dictionaries with references in RimWorld
        private List<Pawn> tmpRelKeys;
        private List<int> tmpRelVals;
        public bool isHighTierCore = false;

        public string sourcePawnName = "Unknown";
        public string scanDate = "Unknown";
        public float brainDamagePercent = 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref storedSkills, "storedSkills", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref storedTraits, "storedTraits", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref strongRelations, "strongRelations", LookMode.Reference, LookMode.Value, ref tmpRelKeys, ref tmpRelVals);
            Scribe_Collections.Look(ref formerLovers, "formerLovers", LookMode.Reference);
            Scribe_Values.Look(ref isHighTierCore, "isHighTierCore", false);

            Scribe_Values.Look(ref sourcePawnName, "sourcePawnName", "Unknown");
            Scribe_Values.Look(ref scanDate, "scanDate", "Unknown");
            Scribe_Values.Look(ref brainDamagePercent, "brainDamagePercent", 0f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (storedSkills == null) storedSkills = new Dictionary<SkillDef, int>();
                if (storedTraits == null) storedTraits = new Dictionary<TraitDef, int>();
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

        public override void CompTick()
        {
            base.CompTick();

            if (parent.IsHashIntervalTick(250))
            {
                if (storedSkills != null && storedSkills.Count > 0 && parent is Building_MechGestator gestator)
                {
                    if (gestator.GestatingMech != null)
                    {
                        var mechComp = gestator.GestatingMech.TryGetComp<CompMechSkillMemory>();
                        if (mechComp != null)
                        {
                            mechComp.skillLevels = new Dictionary<SkillDef, int>(storedSkills);
                            mechComp.traits = new Dictionary<TraitDef, int>(storedTraits);
                            mechComp.strongRelations = new Dictionary<Pawn, int>(strongRelations);
                            mechComp.formerLovers = new List<Pawn>(formerLovers);

                            mechComp.isHighTierCore = isHighTierCore;

                            mechComp.sourcePawnName = sourcePawnName;
                            mechComp.scanDate = scanDate;
                            mechComp.brainDamagePercent = brainDamagePercent;

                            // Clear it out
                            storedSkills.Clear();
                            storedTraits.Clear();
                            strongRelations.Clear();
                            formerLovers.Clear();
                            isHighTierCore = false;
                            sourcePawnName = "Unknown";
                            scanDate = "Unknown";
                            brainDamagePercent = 0f;

                            mechComp.ApplyMemoryToVanillaTracker();
                        }
                    }
                }
            }
        }
    }
}