using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{
    //Code referenced from Character Editor by ISOREX
    public static class HARCompat
    {
        public static bool Active;

        public static bool EnforceRestrictions = true;

        public static Type thingDef_AlienRace;

        private static AccessTools.FieldRef<object, object> alienRace;

        private static AccessTools.FieldRef<object, object> alienPartGenerator;

        private static AccessTools.FieldRef<object, object> generalSettings;

        private static AccessTools.FieldRef<object, List<BodyTypeDef>> bodyTypes;

        public static string Name = "Humanoid Alien Races";

        [UsedImplicitly]
        public static void Activate()
        {
            thingDef_AlienRace = AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");
            alienRace = AccessTools.FieldRefAccess<object>(thingDef_AlienRace, "alienRace");
            Type type2 = AccessTools.Inner(thingDef_AlienRace, "AlienSettings");
            generalSettings = AccessTools.FieldRefAccess<object>(type2, "generalSettings");
            alienPartGenerator = AccessTools.FieldRefAccess<object>(AccessTools.TypeByName("AlienRace.GeneralSettings"), "alienPartGenerator");
            bodyTypes = AccessTools.FieldRefAccess<List<BodyTypeDef>>(AccessTools.TypeByName("AlienRace.AlienPartGenerator"), "bodyTypes");
        }

        public static List<BodyTypeDef> AllowedBodyTypes(Pawn pawn)
        {
            if (thingDef_AlienRace.IsInstanceOfType(pawn.def))
            {
                object obj = alienRace(pawn.def);
                if (obj == null)
                {
                    return null;
                }
                obj = generalSettings(obj);
                if (obj == null)
                {
                    return null;
                }
                obj = alienPartGenerator(obj);
                if (obj == null)
                {
                    return null;
                }
                return bodyTypes(obj);
            }
            return null;
        }

    }
}