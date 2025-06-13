using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Muax36.Rimbody_CombatTrainingCompatibility
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("Harmony_RimbodyGiddyUp");
            try
            {
                Log.Message($"Rimbody Found CombatTraining");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("Rimbody patched CombatTraining ");
            }
            catch (Exception e)
            {
                Log.Error($"Rimbody Failed to patch CombatTraining {e}");
            }
        }
    }
}
