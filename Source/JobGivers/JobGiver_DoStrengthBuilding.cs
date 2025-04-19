using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    internal class JobGiver_DoStrengthBuilding : ThinkNode_JobGiver
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
            if (compPhysique.gain >= compPhysique.gainMax)
            {
                return 0f;
            }

            float result = 5.0f;

            if (compPhysique.useMuscleGoal && compPhysique.MuscleGoal > compPhysique.MuscleMass)
            {
                result += 0.5f + ((compPhysique.MuscleGoal - compPhysique.MuscleMass)/100f);
            }
            else
            {
                result += (25f - compPhysique.MuscleMass) / 100f;
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
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (compPhysique == null)
            {
                return null;
            }

            tmpCandidates.Clear();
            workoutCache.Clear();
            GetSearchSet(pawn, tmpCandidates);
            Predicate<Thing> targetPredicate = delegate (Thing t)
            {
                if (t.IsForbidden(pawn))
                {
                    return false;
                }
                RimbodyDefLists.StrengthTarget.TryGetValue(t.def, out var targetModExtension);
                if(targetModExtension.Type == RimbodyTargetType.Building)
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
                if(RimbodyDefLists.StrengthTarget.TryGetValue(t.def, out var targetModExtension))
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
                            tmpScore = compPhysique.GetScore(RimbodyTargetCategory.Strength, workout, out _);
                        }
                        if (tmpScore > score)
                        {
                            score = tmpScore;
                        }
                        if (score > targethighscore)
                        {
                            targethighscore = score;
                        }
                    }
                    return score;
                }
                return 0;
            }
            Thing thing = null;
            JobDef jobtogive = null;
            thing ??= GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 9999f, targetPredicate, scoreFunc);
            jobtogive = DefOf_Rimbody.Rimbody_DoStrengthBuilding;
            tmpCandidates.Clear();
            workoutCache.Clear();

            if (targethighscore < RimbodyDefLists.strengthHighscore) //If chunk job can be better, try to get chunk job to compare
            {
                bool bodyweightPredicate(Thing t)
                {
                    if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some))
                    {
                        return false;
                    }
                    if (t.IsForbidden(pawn))
                    {
                        return false;
                    }
                    //Todo: ignore stones marked for haul.
                    return true;
                }
                Thing Chunk = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Chunk),
                    PathEndMode.OnCell,
                    TraverseParms.For(pawn, Danger.Some),
                    20f,
                    bodyweightPredicate
                );
                //If chunk available, get the maxing chunk job and its score
                if (Chunk != null)
                {
                    var liftingjobEx = DefOf_Rimbody.Rimbody_DoChunkLifting.GetModExtension<ModExtensionRimbodyJob>();
                    float lifting_score = compPhysique.GetStrengthPartScore(liftingjobEx.strengthParts, liftingjobEx.strength, out _);

                    var pressjobEx = DefOf_Rimbody.Rimbody_DoChunkOverheadPress.GetModExtension<ModExtensionRimbodyJob>();
                    float press_score = compPhysique.GetStrengthPartScore(pressjobEx.strengthParts, pressjobEx.strength, out _);

                    var squatsjobEx = DefOf_Rimbody.Rimbody_DoChunkSquats.GetModExtension<ModExtensionRimbodyJob>();
                    float squats_score = compPhysique.GetStrengthPartScore(squatsjobEx.strengthParts, squatsjobEx.strength, out _);

                    float maxScore = lifting_score;
                    JobDef maxJob = DefOf_Rimbody.Rimbody_DoChunkLifting;
                    int tieCount = 1;

                    // Check press_score
                    if (press_score > maxScore)
                    {
                        maxScore = press_score;
                        maxJob = DefOf_Rimbody.Rimbody_DoChunkOverheadPress;
                        tieCount = 1;
                    }
                    else if (press_score == maxScore) tieCount++;

                    // Check squats_score
                    if (squats_score > maxScore)
                    {
                        maxScore = squats_score;
                        maxJob = DefOf_Rimbody.Rimbody_DoChunkSquats;
                        tieCount = 1;
                    }
                    else if (squats_score == maxScore) tieCount++;

                    // Handle ties only if needed
                    if (tieCount > 1)
                    {
                        int index = 0;
                        if (lifting_score == maxScore) index |= 1;
                        if (press_score == maxScore) index |= 2;
                        if (squats_score == maxScore) index |= 4;

                        // Predefined job arrays for each tie scenario
                        JobDef[] tieJobs = index switch
                        {
                            3 => [DefOf_Rimbody.Rimbody_DoChunkLifting, DefOf_Rimbody.Rimbody_DoChunkOverheadPress],
                            5 => [DefOf_Rimbody.Rimbody_DoChunkLifting, DefOf_Rimbody.Rimbody_DoChunkSquats],
                            6 => [DefOf_Rimbody.Rimbody_DoChunkOverheadPress, DefOf_Rimbody.Rimbody_DoChunkSquats],
                            7 => [DefOf_Rimbody.Rimbody_DoChunkLifting, DefOf_Rimbody.Rimbody_DoChunkOverheadPress, DefOf_Rimbody.Rimbody_DoChunkSquats],
                            _ => [maxJob],
                        };

                        // Random choice from tied top scores
                        maxJob = tieJobs[Rand.Range(0, tieJobs.Length)];
                    }

                    if (targethighscore < maxScore)
                    {
                        thing = Chunk;
                        jobtogive = maxJob;
                    }
                }
            }

            if (thing != null)
            {
                if (jobtogive == DefOf_Rimbody.Rimbody_DoStrengthBuilding)
                {
                    Job job = DoTryGiveTargetJob(pawn, thing);
                    if (job != null)
                    {
                        return job;
                    }
                }
                else
                {
                    Job job = DoTryGiveChunkJob(pawn, thing, jobtogive);
                    if (job != null)
                    {
                        return job;
                    }
                }
            }
            return null;
        }
        public Job DoTryGiveTargetJob(Pawn pawn, Thing t)
        {
            RimbodyDefLists.StrengthTarget.TryGetValue(t.def, out var targetModExtension);
            if (targetModExtension.Type == RimbodyTargetType.Building)
            {
                if (t.def.hasInteractionCell)
                {
                    return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoStrengthBuilding, t, t.InteractionCell);
                }
                else
                {
                    if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var _))
                    {
                        return null;
                    }
                    LocalTargetInfo target = result;
                    if (pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
                    {
                        return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoStrengthBuilding, t, result);
                    }
                }
                return null;
            }
            else
            {
                if (pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some))
                {
                    return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoStrengthLifting, t);
                }
            }
            return null;
        }

        public Job DoTryGiveChunkJob(Pawn pawn, Thing t, JobDef jobtogive)
        {
            if (pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
            {
                return JobMaker.MakeJob(jobtogive, t);
            }
            return null;
        }

        protected virtual void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (RimbodyDefLists.StrengthTarget == null || RimbodyDefLists.StrengthTarget.Count == 0)
            {
                return;
            }
            foreach (var buildingDef in RimbodyDefLists.StrengthTarget.Keys)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(buildingDef));
            }
        }
    }
}