using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JobGiver_DoBalanceBuilding : JobGiver_DoWorkoutBase
    {
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }
            return GetActualPriority(compPhysique);
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveJobActual(pawn, tmpCandidates, thingWorkoutScoreCache);
        }

        public static float GetActualPriority(CompPhysique compPhysique)
        {
            if (compPhysique.gain >= compPhysique.gainMax * RimbodySettings.gainMaxGracePeriod) return 0f;
            if (!compPhysique.parentPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return 0f;
            float result = 4.0f;
            var workoutMemory = compPhysique.memory;
            int target = (int)RimbodyWorkoutCategory.Balance;
            foreach (var item in workoutMemory)
            {
                if ((item >> 16) == target)
                    result -= 0.8f;
                else
                    result += 0.4f;
            }
            return result;
        }
        public static Job TryGiveJobActual(Pawn pawn, List<Thing> tmpCandidates, Dictionary<int, float> thingWorkoutScoreCache)
        {
            var compPhysique = pawn.compPhysique();
            if (compPhysique == null) return null;

            tmpCandidates.Clear();
            thingWorkoutScoreCache.Clear();
            float targethighscore = 0f;
            IntVec3 workoutLocation = IntVec3.Invalid;
            GetSearchSet(pawn, RimbodyDB.BalanceTargets, tmpCandidates);
            float scoreFunc(Thing t)
            {
                RimbodyDB.ThingModExDB.TryGetValue(t.def.shortHash, out var targetModExtension);
                if (thingWorkoutScoreCache.TryGetValue(t.def.shortHash, out float score))
                {
                    if (score > targethighscore)
                    {
                        targethighscore = score;
                    }
                    return score;
                }
                foreach (WorkOut workout in targetModExtension.workouts)
                {
                    if (workout.Category != RimbodyWorkoutCategory.Balance) continue;
                    float tmpScore = (compPhysique.InMemory(workout.id) ? 0.9f : 1f) * compPhysique.GetWorkoutScore(RimbodyWorkoutCategory.Balance, workout);
                    if (tmpScore > score)
                    {
                        score = tmpScore;
                    }
                }
                if (score > targethighscore)
                {
                    targethighscore = score;
                }
                thingWorkoutScoreCache[t.def.shortHash] = score;
                return score;
            }
            Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 9999f, t => TargetValidator(pawn, t), scoreFunc);
            tmpCandidates.Clear();
            thingWorkoutScoreCache.Clear();

            if ((RimbodySettings.useFatigue && targethighscore < RimbodyDB.balanceHighscore) || (!RimbodySettings.useFatigue && targethighscore == 0))
            {
                RimbodyDB.JobModExDB.TryGetValue(DefOf_Rimbody.Rimbody_DoBodyWeightPlank.shortHash, out var plankjobEx);
                float plank_score = (compPhysique.InMemory(plankjobEx.id) ? 0.9f : 1f) * compPhysique.GetBalanceJobScore(plankjobEx.strengthParts, plankjobEx.strength);
                if (targethighscore < plank_score)
                {
                    workoutLocation = Rimbody_Utility.FindWorkoutSpot(pawn, true, DefOf_Rimbody.Rimbody_ExerciseMat, out Thing mattress, 1, 40f);
                    if (workoutLocation != IntVec3.Invalid)
                    {
                        return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoBodyWeightPlank, workoutLocation, mattress);
                    }
                }
            }

            if (thing != null)
            {
                return DoTryGiveTargetJob(pawn, thing);
            }
            return null;
        }

        public static Job DoTryGiveTargetJob(Pawn pawn, Thing t)
        {
            RimbodyDB.ThingModExDB.TryGetValue(t.def.shortHash, out var targetModExtension);
            if (targetModExtension.Type == RimbodyTargetType.Building)
            {
                if (t.def.hasInteractionCell)
                {
                    return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoBalanceBuilding, t, t.InteractionCell);
                }
                else
                {
                    if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var chair))
                    {
                        //This should never happen because TargetValidator has already checked Cell existence
                        return null;
                    }
                    LocalTargetInfo target = result;
                    if (pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
                    {
                        return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoBalanceBuilding, t, result, chair);
                    }
                }
                return null;
            }
            else
            {
                return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoBalanceLifting, t);
            }
            return null;
        }
    }
}
