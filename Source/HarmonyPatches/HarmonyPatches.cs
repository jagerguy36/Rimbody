using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        public static readonly IEnumerable<ThingDef> beautyPlants;

        static HarmonyPatches()
        {
            var harmony = new Harmony("rimworld.mod.Maux.Rimbody");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
