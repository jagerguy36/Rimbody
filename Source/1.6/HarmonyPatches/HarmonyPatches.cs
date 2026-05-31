using HarmonyLib;
using System.Reflection;
using Verse;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("rimworld.mod.Maux.Rimbody");
            harmony.PatchAllUncategorized(Assembly.GetExecutingAssembly());
            if (!Rimbody.CombatExtendedLoaded)
            {
                harmony.PatchCategory("NonCE");
            }
            LongEventHandler.QueueLongEvent(() => CompToHumanlikes.GenerateRaceSettings(true), "Rimbody_InjectPhysique", false, null);
        }
    }
}
