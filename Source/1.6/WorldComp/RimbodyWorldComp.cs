using RimWorld.Planet;
using System;
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
}