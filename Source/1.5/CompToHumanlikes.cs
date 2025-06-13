using Verse;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public static class CompToHumanlikes
    {
        static CompToHumanlikes()
        {
            AddCompToHumanlikes();
        }

        public static void AddCompToHumanlikes()
        {
            foreach (var allDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (allDef.race is { intelligence: Intelligence.Humanlike } && !allDef.IsCorpse)
                {
                    if (allDef.race.IsFlesh == false)
                    {
                        return;
                    }
                    if (!HARCompat.Active || HARCompat.CompatibleRace(allDef))
                    {
                        allDef.comps.Add(new CompProperties_Physique());
                    }
                }
            }
        }
    }
}
