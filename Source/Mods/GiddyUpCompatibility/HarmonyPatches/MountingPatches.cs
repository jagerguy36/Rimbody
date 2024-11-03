using GiddyUp;
using HarmonyLib;
using Maux36.Rimbody;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Muax36.Rimbody_GiddyUpCompatibility
{

    [HarmonyPatch(typeof(CompPhysique), "PhysiqueTick")]
    public class CompPhysique_PhysiqueTick_Patch
    {
        public static bool Prefix(CompPhysique __instance, float forcedCardio, float forcedStrength)
        {
            var pawn = __instance.parent as Pawn;
            if (pawn is not null && pawn.pather?.MovingNow == true && ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber))
            {
                forcedCardio = 0.3f;
                forcedStrength = 0.2f;
                return true;
            }
            return true;
            
        }
    }
}
