using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody_DubsBadHygiene
{

    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("Harmony_RimbodyDBH");
            try
            {
                Log.Message($"Rimbody Found DBH");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("Rimbody patched DBH");
            }
            catch (Exception e)
            {
                Log.Error($"Rimbody Failed to patch DBH {e}");
            }
        }
    }
}
