using UnityEngine;
using Verse;

namespace GITM
{
    public class GITMSettings : ModSettings
    {
        // Default values
        public int standardSkillCap = 14;
        public float standardLearningRate = 0f;
        public float highLearningRate = 0.25f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref standardSkillCap, "standardSkillCap", 14);
            Scribe_Values.Look(ref standardLearningRate, "standardLearningRate", 0f);
            Scribe_Values.Look(ref highLearningRate, "highLearningRate", 0.25f);
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

            // Skill Cap Setting
            listingStandard.Label($"Standard Subcore Skill Cap: {settings.standardSkillCap}");
            settings.standardSkillCap = (int)listingStandard.Slider(settings.standardSkillCap, 0f, 20f);

            listingStandard.Gap();

            // Learning Rate Settings
            listingStandard.Label($"Standard Mech Learning Rate: {settings.standardLearningRate.ToStringPercent()}");
            settings.standardLearningRate = listingStandard.Slider(settings.standardLearningRate, 0f, 2f);

            listingStandard.Label($"High Mech Learning Rate: {settings.highLearningRate.ToStringPercent()}");
            settings.highLearningRate = listingStandard.Slider(settings.highLearningRate, 0f, 2f);

            listingStandard.Gap();
            listingStandard.Label("Note: Learning rates are adjusted to compensate for the lack of passions (x0.35 multiplier). Setting config to 100% will make a mech learn as fast as a pawn with a passion. Mechs don't forget skills.");

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Ghost In The Machine";
        }
    }
}