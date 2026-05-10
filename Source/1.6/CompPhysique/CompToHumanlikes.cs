using Verse;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public static class CompToHumanlikes
    {
        static CompToHumanlikes()
        {
            GenerateRaceSettings(true);
        }
        public static void GenerateRaceSettings(bool addComp = false)
        {
            var dictionary = RimbodySettings.raceOption;
            dictionary ??= [];
            foreach (var allDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (allDef.race is { intelligence: Intelligence.Humanlike } && !allDef.IsCorpse)
                {
                    if (allDef.race.IsFlesh == false)
                        continue;
                    if (!HARCompat.Active || HARCompat.CompatibleRace(allDef))
                    {
                        string raceKey = GetRaceKey(allDef);
                        if (!dictionary.TryGetValue(raceKey, out var value))
                        {
                            value = new RaceSetting
                            {
                                label = (allDef.label ?? "null"),
                                defName = allDef.defName,
                                modName = (allDef.modContentPack?.Name ?? "null"),
                                isRimbodyEnabled = true
                            };
                            dictionary.Add(raceKey, value);
                        }
                        if (addComp)
                        {
                            allDef.comps.Add(new CompProperties_Physique());
                            PhysiqueCacheManager.TrackingDefHashSet.Add(allDef.shortHash);
                        }
                    }
                }
            }
        }
        public static string GetRaceKey(ThingDef thingDef)
        {
            return (thingDef.modContentPack?.PackageId ?? "empty_mod_id") + "-" + thingDef.defName;
        }
    }
}
