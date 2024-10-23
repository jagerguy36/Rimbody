﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimbody.BigAndSmallCompatibility
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
                Log.Message("Rimbody patched BigAndSmall ");
            }
            catch (Exception e)
            {
                Log.Error($"Rimbody Failed to patch BigAndSmall {e}");
            }
        }
    }
}
