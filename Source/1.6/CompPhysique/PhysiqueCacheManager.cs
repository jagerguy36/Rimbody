using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{
    public static class PhysiqueCacheManager
    {
        private static readonly Dictionary<int, CompPhysique> CompPhysiqueCache = [];
        public static HashSet<int> TrackingDefHashSet = [];

        public static CompPhysique GetCompPhysiqueCached(Pawn pawn)
        {
            if (pawn == null) return null;
            if (CompPhysiqueCache.TryGetValue(pawn.thingIDNumber, out CompPhysique comp))
            {
                return comp;
            }
            if(!TrackingDefHashSet.Contains(pawn.def.shortHash))
            {
                return null;
            }
            comp = pawn.TryGetComp<CompPhysique>();
            if (comp != null)
            {
                CompPhysiqueCache.Add(pawn.thingIDNumber, comp);
            }
            return comp;
        }

        public static void ClearCacheForPawn(Pawn pawn)
        {
            CompPhysiqueCache.Remove(pawn.thingIDNumber);
        }

        public static void ClearAllCache()
        {
            CompPhysiqueCache.Clear();
        }
    }
}
