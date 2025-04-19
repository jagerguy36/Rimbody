using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    internal class JobGiver_DoCardioBuilding : ThinkNode_JobGiver
    {
        private static List<Thing> tmpCandidates = [];
        private static Dictionary<string, float> workoutCache = new Dictionary<string, float>();
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }

            float result = 5.0f;

            if (compPhysique.useFatgoal && compPhysique.FatGoal < compPhysique.BodyFat)
            {
                result += 0.5f + ((compPhysique.BodyFat - compPhysique.FatGoal)/100f);
            }
            else
            {
                result += (compPhysique.BodyFat - 25f) / 100f;
            }

            return result;
        }

        public static bool TooTired(Pawn actor)
        {
            if (((actor != null) & (actor.needs != null)) && actor.needs.rest != null && (double)actor.needs.rest.CurLevel < 0.2f)
            {
                return true;
            }
            return false;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Downed || pawn.Drafted)
            {
                return null;
            }
            if (TooTired(pawn))
            {
                return null;
            }
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (compPhysique == null)
            {
                return null;
            }

            //Joggers will always try to jog if possible.
            if(compPhysique.isJogger && JoyUtility.EnjoyableOutsideNow(pawn))
            {
                if (JobDriver_Jogging.TryFindNatureJoggingTarget(pawn, out var interestTarget))
                {
                    Job job = JobMaker.MakeJob(DefOf_Rimbody.Rimbody_Jogging, interestTarget);
                    job.locomotionUrgency = LocomotionUrgency.Sprint;
                    return job;
                }
            }

            //If not joggers or impossible to jog outside
            tmpCandidates.Clear();
            workoutCache.Clear();
            GetSearchSet(pawn, tmpCandidates);
            Predicate<Thing> predicate = delegate (Thing t)
            {
                if (t.IsForbidden(pawn))
                {
                    return false;
                }
                RimbodyDefLists.CardioTarget.TryGetValue(t.def, out var targetModExtension);
                if (targetModExtension.Type == RimbodyTargetType.Building)
                {
                    if (!pawn.CanReserve(t))
                    {
                        return false;
                    }
                    if (t.def.hasInteractionCell)
                    {
                        if (!pawn.CanReserveSittableOrSpot(t.InteractionCell))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var chair))
                        {
                            return false;
                        }
                        LocalTargetInfo target = result;
                        if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
                        {
                            return false;
                        }
                    }
                    return t.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;
                }
                else
                {
                    if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some))
                    {
                        return false;
                    }
                    return true;
                }
            };
            float targethighscore = 0f;
            float scoreFunc(Thing t)
            {
                if (RimbodyDefLists.CardioTarget.TryGetValue(t.def, out var targetModExtension))
                {
                    float score = 0f;
                    foreach (WorkOut workout in targetModExtension.workouts)
                    {
                        float tmpScore = 0;
                        if (workoutCache.ContainsKey(workout.name))
                        {
                            tmpScore = workoutCache[workout.name];
                        }
                        else
                        {
                            tmpScore = compPhysique.GetScore(RimbodyTargetCategory.Cardio, workout, out _);
                        }
                        if (tmpScore > score)
                        {
                            score = tmpScore;
                        }
                        if (score > targethighscore)
                        {
                            targethighscore = score;
                        }
                        Log.Message($"score for{workout.name} is {tmpScore}");
                    }
                    
                    return score;
                }
                return 0;
            }
            Thing thing = null;
            thing ??= GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 9999f, predicate, scoreFunc);
            tmpCandidates.Clear();
            workoutCache.Clear();

            if (targethighscore < RimbodyDefLists.cardioHighscore)
            {
                if (!compPhysique.isJogger && JoyUtility.EnjoyableOutsideNow(pawn)) //Already checked outside condition for jogger.
                {
                    if (JobDriver_Jogging.TryFindNatureJoggingTarget(pawn, out var interestTarget))
                    {
                        //jogging is possible. Compare the score
                        var joggingEx = DefOf_Rimbody.Rimbody_Jogging.GetModExtension<ModExtensionRimbodyJob>();
                        var joggingscore = joggingEx.cardio;
                        if (targethighscore < joggingscore)
                        {
                            Job job = JobMaker.MakeJob(DefOf_Rimbody.Rimbody_Jogging, interestTarget);
                            job.locomotionUrgency = LocomotionUrgency.Sprint;
                            return job;
                        }

                    }
                }
            }

            if (thing != null)
            {
                Job job = DoTryGiveJob(pawn, thing);
                if (job != null)
                {
                    return job;
                }
            }
            return null;
        }

        public Job DoTryGiveJob(Pawn pawn, Thing t)
        {
            RimbodyDefLists.CardioTarget.TryGetValue(t.def, out var targetModExtension);
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

        protected virtual void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (RimbodyDefLists.CardioTarget == null || RimbodyDefLists.CardioTarget.Count == 0)
            {
                return;
            }
            foreach (var buildingDef in RimbodyDefLists.CardioTarget.Keys)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(buildingDef));
            }
        }
    }
}
