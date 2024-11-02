using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody_CE
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("Harmony_RimbodyCE");
            try
            {
                Log.Message($"Rimbody Found CE");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("Rimbody patched CE ");
            }
            catch (Exception e)
            {
                Log.Error($"Rimbody Failed to patch CE {e}");
            }
        }
    }
}