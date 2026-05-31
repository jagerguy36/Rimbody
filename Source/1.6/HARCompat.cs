using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private static Dictionary<int, bool> _cache = new Dictionary<int, bool>();

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
        public static bool IsAlienRaceDef(ThingDef pawnDef)
        {
            return thingDef_AlienRace.IsInstanceOfType(pawnDef);
        }

        public static List<BodyTypeDef> AllowedBodyTypes(ThingDef pawnDef)
        {
            if (IsAlienRaceDef(pawnDef))
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

            if (_cache.TryGetValue(pawnDef.shortHash, out bool cachedResult))
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

            var human = ThingDefOf.Human;
            var expectedLifestages = human.race.lifeStageAges;

            var allowedBodyTypes = AllowedBodyTypes(pawnDef)?.ToHashSet();
            if (allowedBodyTypes == null)
            {
                _cache[pawnDef.shortHash] = false;
                return false;
            }
            bool allPresent = expectedBodyTypes.All(expected => allowedBodyTypes.Contains(expected));

            var lifestages = pawnDef.race.lifeStageAges;
            //bool ageEqual = lifestages.SequenceEqual(expectedLifestages);
            bool ageEqual = true;
            if (expectedLifestages.Count != lifestages.Count)
            {
                ageEqual = false;
            }
            else
            {
                for (int i = 0; i < expectedLifestages.Count; i++)
                {
                    if (lifestages[i].def != expectedLifestages[i].def || lifestages[i].minAge != expectedLifestages[i].minAge)
                    {
                        ageEqual = false;
                        break;
                    }
                }
            }

            bool isValid = allPresent && ageEqual;
            _cache[pawnDef.shortHash] = isValid;
            //Log.Message($"{pawnDef.defName} | Validicheck:");
            //string shower = string.Join(", ", allowedBodyTypes.Select(thingDef => thingDef.defName));
            //Log.Message("Bodytypes: " + shower);
            //string shower3 = string.Join(", ", lifestages.Select(stage => stage.def.defName));
            //Log.Message("LifeStage defName: " + shower3);
            //string shower4 = string.Join(", ", lifestages.Select(stage => stage.def.defName));
            //Log.Message("LifeStage defName: " + shower3);
            //string shower2 = string.Join(", ", lifestages.Select(stage => stage.minAge));
            //Log.Message("LifeStage minAge: "+shower2);
            //Log.Message($"{pawnDef.defName} bodytypes yes: {allPresent}, life yes: {ageEqual}");
            return isValid;
        }

    }
}