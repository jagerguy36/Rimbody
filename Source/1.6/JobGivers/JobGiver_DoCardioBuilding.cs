using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    public class JobGiver_DoCardioBuilding : ThinkNode_JobGiver
    {
        private static List<Thing> tmpCandidates = [];
        private static Dictionary<int, float> workoutCache = new Dictionary<int, float>();
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }
            return GetActualPriority(compPhysique);
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

        protected override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveJobActual(pawn, tmpCandidates, workoutCache);
        }

        public static Job TryGiveJobActual(Pawn pawn, List<Thing> tmpCandidates, Dictionary<int, float> workoutCache)
        {
            var compPhysique = pawn.compPhysique();
            if (compPhysique == null) return null;

            //Joggers will always try to jog if possible.
            if (compPhysique.isJogger && JoyUtility.EnjoyableOutsideNow(pawn))
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
            workoutCache.Clear();
            GetSearchSet(pawn, tmpCandidates);
            Predicate<Thing> predicate = delegate (Thing t)
            {
                if (t.IsForbidden(pawn)) return false;

                RimbodyDefLists.ThingModExDB.TryGetValue(t.def.shortHash, out var targetModExtension);
                if (targetModExtension.Type == RimbodyTargetType.Building)
                {
                    if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) return false;
                    if (pawn.Map.reservationManager.IsReserved(t)) return false;
                    if (!pawn.CanReserve(t, ignoreOtherReservations: true)) return false;
                    if (t.def.hasInteractionCell)
                    {
                        if (!pawn.CanReserveSittableOrSpot(t.InteractionCell)) return false;
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
            };
            float targethighscore = 0f;
            float scoreFunc(Thing t)
            {
                if (RimbodyDefLists.ThingModExDB.TryGetValue(t.def.shortHash, out var targetModExtension))
                {
                    float score = 0f;
                    if (workoutCache.ContainsKey(t.def.shortHash))
                    {
                        score = workoutCache[t.def.shortHash];
                        if (score > targethighscore)
                        {
                            targethighscore = score;
                        }
                        return score;
                    }
                    foreach (WorkOut workout in targetModExtension.workouts)
                    {
                        if (workout.Category != RimbodyWorkoutCategory.Cardio)
                        {
                            continue;
                        }
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
                    return score;
                }
                return 0;
            }
            Thing thing = null;
            thing ??= GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 9999f, predicate, scoreFunc);
            tmpCandidates.Clear();
            workoutCache.Clear();

            if ((RimbodySettings.useFatigue && targethighscore < RimbodyDefLists.cardioHighscore) || (!RimbodySettings.useFatigue && targethighscore == 0))
            {
                if (!compPhysique.isJogger && JoyUtility.EnjoyableOutsideNow(pawn)) //Already checked outside condition for jogger.
                {
                    if (JobDriver_Jogging.TryFindNatureJoggingTarget(pawn, out var interestTarget))
                    {
                        //jogging is possible. Compare the score
                        var joggingEx = RimbodyDefLists.JobModExDB.TryGetValue(DefOf_Rimbody.Rimbody_Jogging.shortHash);
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
            RimbodyDefLists.ThingModExDB.TryGetValue(t.def.shortHash, out var targetModExtension);
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

        protected static void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (RimbodyDefLists.CardioTargets == null || RimbodyDefLists.CardioTargets.Count == 0) return;
            foreach (var buildingDef in RimbodyDefLists.CardioTargets)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(buildingDef));
            }
        }
    }
}
