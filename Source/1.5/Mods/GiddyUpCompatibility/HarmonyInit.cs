using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody_GiddyUpCompatibility
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("Harmony_RimbodyGiddyUp");
            try
            {
                Log.Message($"Rimbody Found GiddyUp");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("Rimbody patched GiddyUp ");
            }
            catch (Exception e)
            {
                Log.Error($"Rimbody Failed to patch GiddyUp {e}");
            }
        }
    }
}
