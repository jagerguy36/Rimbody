using RimWorld;
using Prepatcher;
using Verse;

namespace Maux36.Rimbody
{
    public static class PawnExtensions
    {
        /// <summary>
        /// Injected prepatcher Gizmo field on Building_Door object.
        /// </summary>
        [PrepatcherField]
        [Prepatcher.DefaultValue(0f)]
        public static extern ref float PawnBodyAngleOverride(this Pawn target);
    }
}
