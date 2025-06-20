using HarmonyLib;
using System.Collections.Generic;
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
        }
    }
}
