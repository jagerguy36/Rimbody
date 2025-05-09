using RimWorld;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    internal class Toils_Rimbody
    {
        public static Toil GotoSpotToWorkout(TargetIndex benchIndex, ItemSpot spot = ItemSpot.None)
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
                    spotThingDef = DefOf_Rimbody.Rimbody_ExerciseMats;
                }
                workoutLocation = Rimbody_Utility.FindWorkoutSpot(actor, lookForSpot, spotThingDef, out Thing foundSeat, maxPawns);
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
