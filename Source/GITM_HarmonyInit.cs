using Verse;
using HarmonyLib;

namespace GITM
{
    [StaticConstructorOnStartup]
    public static class GITM_HarmonyInit
    {
        static GITM_HarmonyInit()
        {
            var harmony = new Harmony("com.blackphoenix.ghostinthemachine");
            //Harmony.DEBUG = true;
            harmony.PatchAll();
        }
    }
}