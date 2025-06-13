using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody_VFEAndroidCompatibility
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("Harmony_RimbodyVFEAndroid");
            try
            {
                Log.Message($"Rimbody Found VFEAndroid");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("Rimbody patched VFEAndroid");
            }
            catch (Exception e)
            {
                Log.Error($"Rimbody Failed to patch VFEAndroid {e}");
            }
        }
    }
}
