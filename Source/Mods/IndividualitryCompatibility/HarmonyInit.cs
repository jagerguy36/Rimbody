using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimbody.Individuality
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("Harmony_RimbodyIndividuality");
            try
            {
                Log.Message($"Rimbody Found Individuality");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("Rimbody patched Individuality ");
            }
            catch (Exception e)
            {
                Log.Error($"Rimbody Failed to patch Individuality {e}");
            }
        }
    }
}