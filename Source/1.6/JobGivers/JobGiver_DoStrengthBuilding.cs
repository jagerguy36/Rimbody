using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JobGiver_DoStrengthBuilding : JobGiver_DoWorkoutBase
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
            float result = 5.0f;
            if (compPhysique.useMuscleGoal && compPhysique.MuscleGoal > compPhysique.MuscleMass)
            {
                result += 0.5f + ((compPhysique.MuscleGoal - compPhysique.MuscleMass)* 0.01f);
            }
            else
            {
                result += (25f - compPhysique.MuscleMass) * 0.01f;
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
            JobDef jobDefToGive = null;
            GetSearchSet(pawn, RimbodyDB.StrengthTargets, tmpCandidates);
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
                    if (workout.Category != RimbodyWorkoutCategory.Strength) continue;
                    float tmpScore = (compPhysique.InMemory(workout.id) ? 0.9f : 1f) * compPhysique.GetWorkoutScore(RimbodyWorkoutCategory.Strength, workout);
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
            if (thing != null)
            {
                jobDefToGive = DefOf_Rimbody.Rimbody_DoStrengthBuilding;
            }
            tmpCandidates.Clear();
            thingWorkoutScoreCache.Clear();

            if ((RimbodySettings.useFatigue && targethighscore < RimbodyDB.strengthHighscore) || (!RimbodySettings.useFatigue && thing == null)) //If chunk job can be better, try to get chunk job to compare
            {
                float maxScore = -1f;
                int tieCount = 0;
                JobDef bestJobDef = null;
                bool chunkPredicate(Thing t)
                {
                    if (!pawn.CanReserve(t)) return false;
                    if (t.IsForbidden(pawn)) return false;
                    if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Haul) != null) return false;
                    return true;
                }
                Thing Chunk = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Chunk), PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 20f, chunkPredicate);
                workoutLocation = Rimbody_Utility.FindWorkoutSpot(pawn, true, DefOf_Rimbody.Rimbody_ExerciseMat, out Thing mattress, 1, 40f, Chunk);
                bool nearbyspotFound = workoutLocation != IntVec3.Invalid;

                if (nearbyspotFound)
                {
                    foreach (var strengthJobdef in RimbodyDB.StrengthNontargetJobs)
                    {
                        if (RimbodyDB.ChunkJobHash.Contains(strengthJobdef.shortHash) && Chunk == null) continue;
                        var strengthEx = RimbodyDB.JobModExDB.TryGetValue(strengthJobdef.shortHash);
                        float nonTarget_score = (compPhysique.InMemory(strengthEx.id) ? 0.9f : 1f) * compPhysique.GetStrengthJobScore(strengthEx.strengthParts, strengthEx.strength);
                        if (nonTarget_score > maxScore)
                        {
                            maxScore = nonTarget_score;
                            tieCount = 1;
                            bestJobDef = strengthJobdef;
                        }
                        else if (nonTarget_score == maxScore)
                        {
                            tieCount++;
                            if (Rand.Chance(1f / tieCount))
                            {
                                bestJobDef = strengthJobdef; // Reservoir sampling for random among ties
                            }
                        }
                    }
                }
                if (maxScore > targethighscore)
                {
                    jobDefToGive = bestJobDef;
                    if (RimbodyDB.ChunkJobHash.Contains(bestJobDef.shortHash))
                    {
                        thing = Chunk;
                    }
                    else
                    {
                        thing = mattress;
                    }
                }
            }

            if (jobDefToGive != null)
            {
                if (jobDefToGive == DefOf_Rimbody.Rimbody_DoStrengthBuilding)
                {
                    return DoTryGiveTargetJob(pawn, thing);
                }
                else if (RimbodyDB.ChunkJobHash.Contains(jobDefToGive.shortHash))
                {
                    return DoTryGiveChunkJob(pawn, thing, jobDefToGive);
                }
                else
                {
                    if (workoutLocation != IntVec3.Invalid)
                    {
                        return JobMaker.MakeJob(jobDefToGive, workoutLocation, thing);
                    }
                }
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
                    return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoStrengthBuilding, t, t.InteractionCell);
                }
                else
                {
                    if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var _))
                    {
                        //This should never happen because TargetValidator has already checked Cell existence
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
                return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoStrengthLifting, t);
            }
        }

        public static Job DoTryGiveChunkJob(Pawn pawn, Thing t, JobDef jobDefToGive)
        {
            return JobMaker.MakeJob(jobDefToGive, t);
        }
    }
}