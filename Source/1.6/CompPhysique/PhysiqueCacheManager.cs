using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{
    public static class PhysiqueCacheManager
    {
        private static readonly Dictionary<Pawn, CompPhysique> CompPhysiqueCache = new Dictionary<Pawn, CompPhysique>();
        public static HashSet<string> TrackingDef = new HashSet<string> { };

        public static CompPhysique GetCompPhysiqueCached(Pawn pawn)
        {
            if (pawn == null) return null;
            if (CompPhysiqueCache.TryGetValue(pawn, out CompPhysique comp))
            {
                return comp;
            }
            if(!TrackingDef.Contains(pawn.def?.defName))
            {
                return null;
            }
            comp = pawn.TryGetComp<CompPhysique>();
            if (comp != null)
            {
                CompPhysiqueCache.Add(pawn, comp);
            }
            return comp;
        }

        public static void ClearCacheForPawn(Pawn pawn)
        {
            CompPhysiqueCache.Remove(pawn);
        }

        public static void ClearAllCache()
        {
            CompPhysiqueCache.Clear();
        }
    }
}
