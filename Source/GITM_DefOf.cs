using RimWorld;
using Verse;

namespace GITM
{
    [DefOf]
    public static class GITM_DefOf
    {
        public static JobDef GITM_WatchOverTarget;
        public static JobDef GITM_UnnervingPractice;
        public static JobDef GITM_UnnervingStalking;
        public static JobDef GITM_ScrapeWall;
        public static JobDef GITM_WashAway;
        public static JobDef GITM_ErraticPacing;
        public static JobDef GITM_ObserveFire;
        public static JobDef GITM_ObserveCorpse;
        public static JobDef GITM_ObserveDrugs;
        public static JobDef GITM_ObserveTheNight;
        public static JobDef GITM_ObserveFood;
        public static JobDef GITM_MechDrawOnFloor;
        public static JobDef GITM_GoUnderground;
        public static JobDef GITM_PlayWithAnimal;
        public static ThoughtDef GITM_WatchedWhileSleeping;
        public static ThoughtDef GITM_DisturbedSleepMalicious;
        public static ThoughtDef GITM_UnnervingStalkingThought;
        public static ThoughtDef GITM_DisturbingMechThought;
        public static MentalStateDef GITM_RescuingPawn;
        public static MentalStateDef GITM_DysphoricHorrorState;
        public static MentalStateDef GITM_PeacefulDeparture;
        public static MentalStateDef WanderConfused;
        public static TraitDef GITM_Child;
        public static TraitDef TorturedArtist;
        public static TraitDef Cannibal;
        public static TraitDef NightOwl;
        public static TraitDef Gourmand;
        public static HediffDef GITM_DigitalBrainDamage;

        static GITM_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GITM_DefOf));
        }
    }
}