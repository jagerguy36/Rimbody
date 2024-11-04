﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody
{

    [HarmonyPatch(typeof(Pawn), "Notify_AddBedThoughts")]
    public static class Pawn_Notify_AddBedThoughts
    {

        static void Postfix(Pawn __instance)
        {
            if(__instance != null)
            {
                var compPhysique = __instance.TryGetComp<CompPhysique>();
                compPhysique?.memory.Clear();
            }           
        }
    }
}
