using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{
    public class RimbodyWorldComp : WorldComponent
    {
        public RimbodyWorldComp(World world) : base(world)
        {
        }

        public override void FinalizeInit(bool fromload)
        {
            base.FinalizeInit(fromload);
            try
            {
                PhysiqueCacheManager.ClearAllCache(); //clear any pawns from a previous world 
            }
            catch (Exception)
            {
                Log.Error($"Rimbody is unable to clear all comp caches on world initialization!");
            }
        }
    }

    public static class PhysiqueCacheManager
    {
        private static readonly Dictionary<Pawn, CompPhysique> CompPhysiqueCache = new Dictionary<Pawn, CompPhysique>();

        public static CompPhysique GetCompPhysiqueCached(Pawn pawn)
        {
            if (pawn == null) return null;
            if (CompPhysiqueCache.TryGetValue(pawn, out CompPhysique comp))
            {
                return comp;
            }
            comp = pawn.GetComp<CompPhysique>();
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