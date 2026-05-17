using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JobGiver_DoCardioBuilding : JobGiver_DoWorkoutBase
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
            float result = 5.0f;
            if (compPhysique.useFatgoal && compPhysique.FatGoal < compPhysique.BodyFat)
            {
                result += 0.5f + ((compPhysique.BodyFat - compPhysique.FatGoal)/100f);
            }
            else
            {
                result += (compPhysique.BodyFat - 25f) * 0.01f;
            }
            return result;
        }
        public static Job TryGiveJobActual(Pawn pawn, List<Thing> tmpCandidates, Dictionary<int, float> thingWorkoutScoreCache)
        {
            var compPhysique = pawn.compPhysique();
            if (compPhysique == null) return null;

            //Joggers will always try to jog if possible.
            bool canJogNow = JoyUtility.EnjoyableOutsideNow(pawn);
            if (canJogNow && compPhysique.isJogger)
            {
                if (JobDriver_Jogging.TryFindNatureJoggingTarget(pawn, out var interestTarget))
                {
                    Job job = JobMaker.MakeJob(DefOf_Rimbody.Rimbody_Jogging, interestTarget);
                    job.locomotionUrgency = LocomotionUrgency.Sprint;
                    return job;
                }
            }

            //If not joggers or impossible to jog outside, look for cardio targets
            tmpCandidates.Clear();
            thingWorkoutScoreCache.Clear();
            GetSearchSet(pawn, RimbodyDB.CardioTargets, tmpCandidates);
            float targethighscore = 0f;
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
                    if (workout.Category != RimbodyWorkoutCategory.Cardio) continue;
                    float tmpScore = compPhysique.GetWorkoutScore(RimbodyWorkoutCategory.Cardio, workout);
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

            if ((RimbodySettings.useFatigue && targethighscore < RimbodyDB.cardioHighscore) || (!RimbodySettings.useFatigue && targethighscore == 0))
            {
                if (canJogNow && !compPhysique.isJogger) //Already checked outside condition for jogger.
                {
                    if (JobDriver_Jogging.TryFindNatureJoggingTarget(pawn, out var interestTarget))
                    {
                        //jogging is possible. Compare the score
                        var joggingEx = RimbodyDB.JobModExDB.TryGetValue(DefOf_Rimbody.Rimbody_Jogging.shortHash);
                        if (joggingEx != null)
                        {
                            float joggingscore = compPhysique.GetCardioJobScore(joggingEx.strengthParts, joggingEx.cardio);
                            if (targethighscore < joggingscore)
                            {
                                Job job = JobMaker.MakeJob(DefOf_Rimbody.Rimbody_Jogging, interestTarget);
                                job.locomotionUrgency = LocomotionUrgency.Sprint;
                                return job;
                            }
                        }
                    }
                }
            }

            if (thing != null)
            {
                Job job = DoTryGiveJob(pawn, thing);
                return job;
            }
            return null;
        }

        public static Job DoTryGiveJob(Pawn pawn, Thing t)
        {
            RimbodyDB.ThingModExDB.TryGetValue(t.def.shortHash, out var targetModExtension);
            if (targetModExtension.Type == RimbodyTargetType.Building)
            {
                if (t.def.hasInteractionCell)
                {
                    return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoCardioBuilding, t, t.InteractionCell);
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
                        return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoCardioBuilding, t, result, chair);
                    }
                }
                return null;
            }
            else
            {
                return null;//cardio item not yet implemented.
                //if (pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some))
                //{
                //    return null;
                //}
            }
        }
    }
}
