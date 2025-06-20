//using Prepatcher;
using Verse;

namespace Maux36.Rimbody
{
    public static class PawnExtensions
    {
        public static void SetPawnBodyAngleOverride(this Pawn pawn, float angle)
        {
            var comp = pawn.compPhysique();
            if (comp != null)
            {
                comp.pawnBodyAngleOverride = angle;
            }
        }

        //[PrepatcherField]
        //[InjectComponent]
        public static CompPhysique compPhysique(this Pawn pawn)
        {
            return PhysiqueCacheManager.GetCompPhysiqueCached(pawn);
        }
    }
}
