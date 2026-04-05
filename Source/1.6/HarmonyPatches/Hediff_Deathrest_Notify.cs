using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody
{
    public class Hediff_Deathrest_Notify
    {
        [HarmonyPatch(typeof(Hediff_Deathrest), "PostAdd")]
        public static class Hediff_Deathrest_PostAdd_Patch
        {
            static void Postfix(Pawn ___pawn)
            {
                var compPhysique = ___pawn.compPhysique();
                compPhysique?.DirtyDeathrestCache();
            }
        }

        [HarmonyPatch(typeof(Hediff_Deathrest), "PostRemoved")]
        public static class Hediff_Deathrest_PostRemoved_Patch
        {
            static void Postfix(Pawn ___pawn)
            {
                var compPhysique = ___pawn.compPhysique();
                compPhysique?.DirtyDeathrestCache();
            }
        }
    }
}
