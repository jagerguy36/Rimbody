using HarmonyLib;
using Maux36.Rimbody;
using System;
using System.Reflection;
using Verse;

namespace Maux36.Rimbody_BigAndSmall
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("Harmony_RimbodyBigAndSmally");
            try
            {
                Log.Message($"Rimbody Found BigAndSmall");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("Rimbody patched BigAndSmall");

                if (ModsConfig.IsActive("redmattis.bigsmall.core"))
                {
                    GeneDef geneDef;
                    geneDef = DefDatabase<GeneDef>.GetNamed("BS_NoFood", false);
                    if (geneDef != null) RimbodyDefLists.GeneFactors[geneDef.shortHash] = (0.85f, 1f, 1f, 1.15f);
                    geneDef = DefDatabase<GeneDef>.GetNamed("BS_NoFood_Hemogenic", false);
                    if (geneDef != null) RimbodyDefLists.GeneFactors[geneDef.shortHash] = (0.85f, 1f, 1f, 1.15f);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Rimbody Failed to patch BigAndSmall {e}");
            }
        }
    }
}
