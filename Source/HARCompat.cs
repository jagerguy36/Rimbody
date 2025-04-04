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
        public static bool Active = false;

        private static Type thingDef_AlienRace;

        private static AccessTools.FieldRef<object, object> alienRace;

        private static AccessTools.FieldRef<object, object> alienPartGenerator;

        private static AccessTools.FieldRef<object, object> generalSettings;

        private static AccessTools.FieldRef<object, List<BodyTypeDef>> bodyTypes;

        private static Dictionary<string, bool> _cache = new Dictionary<string, bool>();

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

        public static List<BodyTypeDef> AllowedBodyTypes(ThingDef pawnDef)
        {
            if (thingDef_AlienRace.IsInstanceOfType(pawnDef))
            {
                object obj = alienRace(pawnDef);
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
        public static bool CompatibleRace(ThingDef pawnDef)
        {

            if (_cache.TryGetValue(pawnDef.defName, out bool cachedResult))
            {
                return cachedResult;
            }

            HashSet<BodyTypeDef> expectedBodyTypes = ModsConfig.BiotechActive ? new HashSet<BodyTypeDef>
            {
                BodyTypeDefOf.Baby,
                BodyTypeDefOf.Child,
                BodyTypeDefOf.Male,
                BodyTypeDefOf.Female,
                BodyTypeDefOf.Thin,
                BodyTypeDefOf.Hulk,
                BodyTypeDefOf.Fat,
            } : new HashSet<BodyTypeDef>
            {
                BodyTypeDefOf.Male,
                BodyTypeDefOf.Female,
                BodyTypeDefOf.Hulk,
                BodyTypeDefOf.Fat,
                BodyTypeDefOf.Thin
            };

            var human = DefDatabase<ThingDef>.GetNamed("Human", true);
            var expectedLifestages = human.race.lifeStageAges;

            var allowedBodyTypes = AllowedBodyTypes(pawnDef).ToHashSet();
            bool allPresent = expectedBodyTypes.All(expected => allowedBodyTypes.Contains(expected));

            var lifestages = pawnDef.race.lifeStageAges;
            bool ageEqual = lifestages.SequenceEqual(expectedLifestages);

            bool isValid = allPresent && ageEqual;
            _cache[pawnDef.defName] = isValid;
            //string shower = string.Join(", ", allowedBodyTypes.Select(thingDef => thingDef.defName));
            //Log.Message(shower);
            //string shower2 = string.Join(", ", expectedBodyTypes.Select(thingDef => thingDef.defName));
            //Log.Message(shower2);
            //Log.Message($"{pawn.Name} bodytypes yes: {allPresent}, life yes: {ageEqual}");
            return isValid;
        }

    }
}