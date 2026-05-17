using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public abstract class JobGiver_DoWorkoutBase: ThinkNode_JobGiver
    {
        protected readonly List<Thing> tmpCandidates = [];
        protected readonly Dictionary<int, float> thingWorkoutScoreCache = [];

        protected static bool TargetValidator(Pawn pawn, Thing t)
        {
            if (t.IsForbidden(pawn)) return false;

            if (!RimbodyDB.ThingModExDB.TryGetValue(t.def.shortHash, out var targetModExtension)) return false;
            if (targetModExtension.Type == RimbodyTargetType.Building)
            {
                if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) return false;
                if (pawn.Map.reservationManager.IsReserved(t)) return false;
                if (!pawn.CanReserve(t, ignoreOtherReservations: true)) return false;
                if (t.def.hasInteractionCell)
                {
                    if (!pawn.CanReserveSittableOrSpot(t.InteractionCell))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var chair)) return false;
                    LocalTargetInfo target = result;
                    if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some, 1, -1, null, false)) return false;
                }
                return t.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;
            }
            else
            {
                if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some)) return false;
                return true;
            }
        }
        protected static void GetSearchSet(Pawn pawn, List<ThingDef> targest, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (targest == null || targest.Count == 0) return;
            foreach (var buildingDef in targest)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(buildingDef));
            }
        }
    }
}
