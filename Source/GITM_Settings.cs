using UnityEngine;
using Verse;

namespace GITM
{
    public class GITMSettings : ModSettings
    {
        public int standardSkillCap = 14;
        public float standardLearningRate = 0f;
        public float highLearningRate = 0.25f;
        public float initialCorpseBrainIntegrity = 1.0f;
        public float finalCorpseBrainIntegrity = 0.0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref standardSkillCap, "standardSkillCap", 14);
            Scribe_Values.Look(ref standardLearningRate, "standardLearningRate", 0f);
            Scribe_Values.Look(ref highLearningRate, "highLearningRate", 0.25f);
            
            Scribe_Values.Look(ref initialCorpseBrainIntegrity, "initialCorpseBrainIntegrity", 1.0f);
            Scribe_Values.Look(ref finalCorpseBrainIntegrity, "finalCorpseBrainIntegrity", 0.0f);
        }
    }

    public class GITM_Mod : Mod
    {
        public static GITMSettings settings;

        public GITM_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<GITMSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label($"Standard Subcore Skill Cap: {settings.standardSkillCap}");
            settings.standardSkillCap = (int)listingStandard.Slider(settings.standardSkillCap, 0f, 20f);
            listingStandard.Gap();

            listingStandard.Label($"Standard Mech Learning Rate: {settings.standardLearningRate.ToStringPercent()}");
            settings.standardLearningRate = listingStandard.Slider(settings.standardLearningRate, 0f, 2f);

            listingStandard.Label($"High Mech Learning Rate: {settings.highLearningRate.ToStringPercent()}");
            settings.highLearningRate = listingStandard.Slider(settings.highLearningRate, 0f, 2f);
            listingStandard.Gap();

            listingStandard.Label($"Initial Corpse Brain Integrity: {settings.initialCorpseBrainIntegrity.ToStringPercent()}");
            settings.initialCorpseBrainIntegrity = listingStandard.Slider(settings.initialCorpseBrainIntegrity, 0f, 10f);

            listingStandard.Label($"Final Corpse Brain Integrity (at Dessicated): {settings.finalCorpseBrainIntegrity.ToStringPercent()}");
            settings.finalCorpseBrainIntegrity = listingStandard.Slider(settings.finalCorpseBrainIntegrity, -10f, 1f);

            listingStandard.Gap();
            listingStandard.Label("Corpses will linearly lose brain integrity as they progress towards rot, accumulating brain damage.\n\nFull rot takes 2.5 days with no refrigeration, and brain damage has essentially no effect under 10%, so for a 'perfect' mech you'll need to scan an unrefrigerated corpse in 6 hours or less (in default settings).");

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "Ghost In The Machine";
    }
}