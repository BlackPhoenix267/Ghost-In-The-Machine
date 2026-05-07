using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GITM
{
    public class CompProperties_MechSkillMemory : CompProperties
    {
        public CompProperties_MechSkillMemory() => this.compClass = typeof(CompMechSkillMemory);
    }

    public class CompMechSkillMemory : ThingComp
    {
        public Dictionary<SkillDef, int> skillLevels = new Dictionary<SkillDef, int>();
        public Dictionary<TraitDef, int> traits = new Dictionary<TraitDef, int>();
        public Dictionary<Pawn, int> strongRelations = new Dictionary<Pawn, int>();
        public List<Pawn> formerLovers = new List<Pawn>();
        // Required temporary lists for saving dictionaries with references in RimWorld
        private List<Pawn> tmpRelKeys;
        private List<int> tmpRelVals;
        public int nextGoUndergroundEligibleTick = 0;
        public int nextMechDrawOnFloorEligibleTick = 0;
        public int nextObserveCorpseEligibleTick = 0;
        public int nextObserveDrugsEligibleTick = 0;
        public int nextObserveFireEligibleTick = 0;
        public int nextObserveFoodEligibleTick = 0;
        public int nextObserveTheNightEligibleTick = 0;
        public int nextPlayWithAnimalsEligibleTick = 0;
        public int nextUnnervingFixationsEligibleTick = 0;
        public int nextWatchOverLovedOnesEligibleTick = 0;
        public int nextHuntSmallAnimalEligibleTick = 0;

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

            Scribe_Values.Look(ref nextGoUndergroundEligibleTick, "nextGoUndergroundEligibleTick", 0);
            Scribe_Values.Look(ref nextMechDrawOnFloorEligibleTick, "nextMechDrawOnFloorEligibleTick", 0);
            Scribe_Values.Look(ref nextObserveCorpseEligibleTick, "nextObserveCorpseEligibleTick", 0);
            Scribe_Values.Look(ref nextObserveDrugsEligibleTick, "nextObserveDrugsEligibleTick", 0);
            Scribe_Values.Look(ref nextObserveFireEligibleTick, "nextObserveFireEligibleTick", 0);
            Scribe_Values.Look(ref nextObserveFoodEligibleTick, "nextObserveFoodEligibleTick", 0);
            Scribe_Values.Look(ref nextObserveTheNightEligibleTick, "nextObserveTheNightEligibleTick", 0);
            Scribe_Values.Look(ref nextPlayWithAnimalsEligibleTick, "nextPlayWithAnimalsEligibleTick", 0);
            Scribe_Values.Look(ref nextUnnervingFixationsEligibleTick, "nextUnnervingFixationsEligibleTick", 0);
            Scribe_Values.Look(ref nextWatchOverLovedOnesEligibleTick, "nextWatchOverLovedOnesEligibleTick", 0);
            Scribe_Values.Look(ref nextHuntSmallAnimalEligibleTick, "nextHuntSmallAnimalEligibleTick", 0);



            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (skillLevels == null) skillLevels = new Dictionary<SkillDef, int>();
                if (traits == null) traits = new Dictionary<TraitDef, int>();
                if (strongRelations == null) strongRelations = new Dictionary<Pawn, int>();
                if (formerLovers == null) formerLovers = new List<Pawn>();
                formerLovers.RemoveAll(x => x == null);

                //If a pawn was totally erased from the game save, RimWorld returns null.
                //We must remove null keys or the dictionary will throw errors.
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

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            ApplyMemoryToVanillaTracker();
        }

        public void ApplyMemoryToVanillaTracker()
        {
            if (skillLevels == null || skillLevels.Count == 0) return;

            if (parent is Pawn pawn)
            {
                if (pawn.skills == null) pawn.skills = new Pawn_SkillTracker(pawn);

                foreach (var kvp in skillLevels)
                {
                    SkillRecord record = pawn.skills.GetSkill(kvp.Key);
                    record.Level = kvp.Value;
                }
                if (brainDamagePercent > 0f)
                {
                    BodyPartRecord mechBrain = pawn.health.hediffSet.GetBrain(); // Fallback to whole body if mech has no brain part defined

                    Hediff damageHediff = pawn.health.hediffSet.GetFirstHediffOfDef(GITM_DefOf.GITM_DigitalBrainDamage);
                    if (damageHediff == null)
                    {
                        damageHediff = pawn.health.AddHediff(GITM_DefOf.GITM_DigitalBrainDamage, mechBrain);
                    }
                    // Set the severity to match the float percentage (e.g. 0.15 for 15%)
                    damageHediff.Severity = brainDamagePercent;
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            // If there's no data, show nothing
            if (!isHighTierCore && sourcePawnName == "Unknown") return null;

            string text = $"Tier: {(isHighTierCore ? "High" : "Standard")}";
            if (sourcePawnName != "Unknown") text += $", Source: {sourcePawnName}";
            if (scanDate != "Unknown") text += $"\nScanned: {scanDate}";

            return text;
        }

        public override void CompTick()
        {
            base.CompTick();

            // To save performance, we only process this check every 250 ticks (a rare tick).
            // I didn't use this method for the jobs since they need to be in the thinkTree,
            // Nor for the other mental breaks since they have specific triggers rather than a general MTB.
            if (!parent.Spawned || !parent.IsHashIntervalTick(250)) return;

            if (parent is Pawn pawn && !pawn.Dead && !pawn.Downed && pawn.Awake() && !pawn.InMentalState)
            {
                if (traits != null && traits.ContainsKey(TraitDefOf.BodyPurist))
                {
                    if (Rand.MTBEventOccurs(10f, 60000f, 250f))
                    {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(
                            GITM_DefOf.GITM_DysphoricHorrorState,
                            "Body Purist Dysphoria",
                            true
                        );
                    }
                }
                if (brainDamagePercent > 0f)
                {
                    // MTB = 1 day / (brain damage percent)^2
                    float mtbDays = 1f / (brainDamagePercent * brainDamagePercent);

                    if (Rand.MTBEventOccurs(mtbDays, 60000f, 250f))
                    {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(
                            GITM_DefOf.WanderConfused,
                            "Fragmented Memory (Digital Brain Damage)",
                            true
                        );
                    }
                }
            }
        }
    }
}