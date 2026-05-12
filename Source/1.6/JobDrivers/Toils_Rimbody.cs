using RimWorld;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    internal class Toils_Rimbody
    {
        public static Toil GotoSpotToWorkout(TargetIndex benchIndex, ItemSpot spot, bool considerCurrent = false)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.CurJob;
                IntVec3 workoutLocation = IntVec3.Invalid;
                bool lookForSpot = false;
                ThingDef spotThingDef = null;
                int maxPawns = 1;
                if (spot == ItemSpot.FlatBench)
                {
                    lookForSpot = true;
                    spotThingDef = DefOf_Rimbody.Rimbody_FlatBench;
                    maxPawns = 2;
                }
                if (spot == ItemSpot.ExerciseMats)
                {
                    lookForSpot = true;
                    spotThingDef = DefOf_Rimbody.Rimbody_ExerciseMat;
                }
                if (considerCurrent)
                {
                    var actorMap = actor.Map;
                    bool standSpotValidator(IntVec3 c)
                    {
                        if (!actor.CanReserve(c)) return false;
                        if (!c.Standable(actorMap)) return false;
                        if (c.GetRegion(actorMap).type == RegionType.Portal) return false;
                        if (c.ContainsStaticFire(actorMap) || c.ContainsTrap(actorMap)) return false;
                        if (actorMap.zoneManager.ZoneAt(c) is Zone_Growing) return false;
                        return true;
                    }
                    if (standSpotValidator(actor.Position))
                    {
                        workoutLocation = actor.Position;
                    }
                }
                Thing foundSeat = null;
                if (workoutLocation == IntVec3.Invalid)
                {
                    workoutLocation = Rimbody_Utility.FindWorkoutSpot(actor, lookForSpot, spotThingDef, out foundSeat, maxPawns);
                }
                if (workoutLocation == IntVec3.Invalid)
                {
                    actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                }
                if (foundSeat != null)
                {
                    curJob.SetTarget(benchIndex, foundSeat);
                    actor.Reserve(foundSeat, actor.CurJob, maxPawns, 0);
                }
                actor.Reserve(workoutLocation, actor.CurJob);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, workoutLocation);
                actor.pather.StartPath(workoutLocation, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }
    }
}
