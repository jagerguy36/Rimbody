using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    //Code referenced from Character Editor by ISOREX
    public static class HARCompat
    {
        public static bool Active;

        private static bool EnforceRestrictions = true;

        private static Type thingDef_AlienRace;

        private static AccessTools.FieldRef<object, object> alienRace;

        private static AccessTools.FieldRef<object, object> alienPartGenerator;

        private static AccessTools.FieldRef<object, object> generalSettings;

        private static AccessTools.FieldRef<object, List<BodyTypeDef>> bodyTypes;

        private static HashSet<BodyTypeDef> expectedBodyTypes = ModsConfig.BiotechActive? new HashSet<BodyTypeDef>
        {
            BodyTypeDefOf.Baby,
            BodyTypeDefOf.Child,
            BodyTypeDefOf.Male,
            BodyTypeDefOf.Female,
            BodyTypeDefOf.Hulk,
            BodyTypeDefOf.Fat
        }: new HashSet<BodyTypeDef>
        {
            BodyTypeDefOf.Male,
            BodyTypeDefOf.Female,
            BodyTypeDefOf.Hulk,
            BodyTypeDefOf.Fat
        };

        private static float[] expectedLifestages = ModsConfig.BiotechActive? new float[] { 0f, 1f, 3f, 9f, 13f, 18f } : new float[] { 0f, 3f, 13f, 18f };

        private static string Name = "Humanoid Alien Races";

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
        public static bool IsHAR(this Pawn pawn)
        {
            return pawn.def.GetType().ToString().StartsWith("AlienRace");
        }
        public static bool CompatibleRace(Pawn pawn)
        {
            var allowedBodyTypes = AllowedBodyTypes(pawn).ToHashSet();
            bool allPresent = expectedBodyTypes.All(expected => allowedBodyTypes.Contains(expected));

            var lifestages = pawn.RaceProps.lifeStageAges.Select(b => b.minAge);
            bool ageEqual = lifestages.SequenceEqual(expectedLifestages);
            return allPresent && ageEqual;
        }

    }
}