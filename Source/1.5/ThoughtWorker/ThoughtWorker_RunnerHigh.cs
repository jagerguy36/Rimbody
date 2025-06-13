using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody
{
    internal class ThoughtWorker_RunnerHigh : ThoughtWorker
    {
        private static readonly TraitDef SpeedOffsetDef = DefDatabase<TraitDef>.GetNamed("SpeedOffset", true);
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.CurJobDef != DefOf_Rimbody.Rimbody_Jogging)
            {
                return ThoughtState.Inactive;
            }
            if (p?.story?.traits?.HasTrait(SpeedOffsetDef, 2) == true)
            {
                return true;
            }
            return ThoughtState.Inactive;
        }
    }
}