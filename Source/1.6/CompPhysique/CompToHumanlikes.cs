using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{
    public static class CompToHumanlikes
    {
        public static void GenerateRaceSettings(bool addComp = false)
        {
            var dictionary = RimbodySettings.raceOption;
            var invalidDefs = new List<string>();
            foreach (var allDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (allDef.race is { intelligence: Intelligence.Humanlike } && !allDef.IsCorpse)
                {
                    if (!IsValidTargetDef(allDef))
                    {
                        invalidDefs.Add(allDef.defName);
                        continue;
                    }
                    //Without HAR, everything is human
                    if (!HARCompat.Active)
                    {
                        if (addComp) AddCompPhysique(allDef);
                        continue;
                    }
                    //With HAR, but this thingdef is not AlienRaceDef (Big and Small generated defs)
                    if (!HARCompat.IsAlienRaceDef(allDef))
                    {
                        if (addComp) AddCompPhysique(allDef);
                        continue;
                    }
                    //with HAR, checking in on AlienDef
                    if (HARCompat.CompatibleRace(allDef))
                    {
                        //Humans go in for free
                        if (allDef.defName == "Human")
                        {
                            if (addComp) AddCompPhysique(allDef);
                            continue;
                        }
                        else
                        {
                            string raceKey = GetRaceKey(allDef);
                            if (!dictionary.TryGetValue(raceKey, out var value))
                            {
                                value = new RaceSetting
                                {
                                    label = (allDef.label ?? "null"),
                                    defName = allDef.defName,
                                    modName = (allDef.modContentPack?.Name ?? "null"),
                                    isRimbodyEnabled = false
                                };
                                dictionary.Add(raceKey, value);
                            }
                            if (addComp && value.isRimbodyEnabled)
                            {
                                allDef.comps.Add(new CompProperties_Physique());
                                PhysiqueCacheManager.TrackingDefHashSet.Add(allDef.shortHash);
                            }
                        }
                    }
                }
            }
            if (dictionary.Count > 0)
            {
                Rimbody.ToggleShowRaceSettings(true);
            }
            if (invalidDefs.Count > 0)
            {
                Log.Message($"[Rimbody] GenerateRaceSettings skipped {invalidDefs.Count} invalid defs: {string.Join(", ", invalidDefs)}");
            }
            if (addComp)
            {
                Log.Message("[Rimbody] Injected Physique to valid races");
            }
        }
        private static void AddCompPhysique(ThingDef def)
        {
            def.comps.Add(new CompProperties_Physique());
            PhysiqueCacheManager.TrackingDefHashSet.Add(def.shortHash);
        }

        public static bool IsValidTargetDef(ThingDef def)
        {
            if (def.race.IsFlesh == false)
                return false;
            return true;
        }
        public static string GetRaceKey(ThingDef thingDef)
        {
            return (thingDef.modContentPack?.PackageId ?? "empty_mod_id") + "-" + thingDef.defName;
        }
    }
}
