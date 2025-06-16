using Prepatcher;
using Verse;

namespace Maux36.Rimbody
{
    public static class PawnExtensions
    {
        private static float _defaultAngle = -1f;
        public static ref float PawnBodyAngleOverride(this Pawn pawn)
        {
            var comp = pawn.compPhysique();
            if (comp == null)
                return ref _defaultAngle;

            return ref comp.pawnBodyAngleOverride;
        }

        [PrepatcherField]
        [InjectComponent]
        public static CompPhysique compPhysique(this Pawn pawn)
        {
            return pawn.GetComp<CompPhysique>();
        }
    }
}
