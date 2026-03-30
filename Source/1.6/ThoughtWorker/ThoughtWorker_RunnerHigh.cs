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
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.CurJobDef == DefOf_Rimbody.Rimbody_Jogging && p.compPhysique()?.isJogger == true)
            {
                return true;
            }
            return ThoughtState.Inactive;
        }
    }
}