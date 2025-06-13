using RimWorld;
using Prepatcher;
using Verse;

namespace Maux36.Rimbody
{
    public static class PawnExtensions
    {
        [PrepatcherField]
        [Prepatcher.DefaultValue(-1f)]
        public static extern ref float PawnBodyAngleOverride(this Pawn target);

        [PrepatcherField]
        [InjectComponent]
        public static extern CompPhysique compPhysique(this Pawn pawn);

    }
}
