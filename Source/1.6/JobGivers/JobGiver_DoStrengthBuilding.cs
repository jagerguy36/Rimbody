using RimWorld;
using System;
using System.Collections.Generic;
using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    public class JobGiver_DoStrengthBuilding : ThinkNode_JobGiver
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
            if (compPhysique.gain >= compPhysique.gainMax * RimbodySettings.gainMaxGracePeriod)
            {
                return 0f;
            }
            return GetActualPriority(compPhysique);

        }

        public static float GetActualPriority(CompPhysique compPhysique)
        {
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

        protected override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveJobActual(pawn, tmpCandidates, workoutCache);
        }

        public static Job TryGiveJobActual(Pawn pawn, List<Thing> tmpCandidates, Dictionary<int, float> workoutCache)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return null;
            var compPhysique = pawn.compPhysique();
            if (compPhysique == null) return null;

            tmpCandidates.Clear();
            workoutCache.Clear();
            GetSearchSet(pawn, tmpCandidates);
            Predicate<Thing> targetPredicate = delegate (Thing t)
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
                        if (workout.Category != RimbodyWorkoutCategory.Strength)
                        {
                            continue;
                        }
                        float tmpScore = (compPhysique.memory.Contains("strength|" + workout.name) ? 0.9f : 1f) * compPhysique.GetWorkoutScore(RimbodyWorkoutCategory.Strength, workout);
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
            IntVec3 workoutLocation = IntVec3.Invalid;
            JobDef jobtogive = null;
            thing ??= GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 9999f, targetPredicate, scoreFunc);
            if (thing != null)
            {
                jobtogive = DefOf_Rimbody.Rimbody_DoStrengthBuilding;
            }
            tmpCandidates.Clear();
            workoutCache.Clear();

            if ((RimbodySettings.useFatigue && targethighscore < RimbodyDefLists.strengthHighscore) || (!RimbodySettings.useFatigue && thing == null)) //If chunk job can be better, try to get chunk job to compare
            {
                float maxScore = -1f;
                int tieCount = 0;
                JobDef bestJob = null;
                bool chunkPredicate(Thing t)
                {
                    //if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some)) return false;
                    if (!pawn.CanReserve(t)) return false;
                    if (t.IsForbidden(pawn)) return false;
                    if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Haul) != null) return false;
                    return true;
                }
                Thing Chunk = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Chunk), PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 20f, chunkPredicate);
                workoutLocation = Rimbody_Utility.FindWorkoutSpot(pawn, true, DefOf_Rimbody.Rimbody_ExerciseMat, out Thing mattress, 1, 40f);
                bool nearbyspotFound = workoutLocation != IntVec3.Invalid;

                if (nearbyspotFound)
                {
                    foreach (var strengthJobdef in RimbodyDefLists.StrengthNontargetJobs)
                    {
                        var strengthEx = RimbodyDefLists.JobModExDB.TryGetValue(strengthJobdef.shortHash);
                        if (strengthJobdef.defName.StartsWith("Rimbody_DoChunk") && Chunk == null)
                        {
                            continue;
                        }
                        float nonTarget_score = (compPhysique.memory.Contains("strength|" + strengthJobdef.defName) ? 0.9f : 1f) * compPhysique.GetStrengthJobScore(strengthEx.strengthParts, strengthEx.strength);
                        if (nonTarget_score > maxScore)
                        {
                            maxScore = nonTarget_score;
                            tieCount = 1;
                            bestJob = strengthJobdef;
                        }
                        else if (nonTarget_score == maxScore)
                        {
                            tieCount++;
                            if (Rand.Chance(1f / tieCount))
                            {
                                bestJob = strengthJobdef; // Reservoir sampling for random among ties
                            }
                        }
                    }
                }
                if (maxScore > targethighscore)
                {
                    jobtogive = bestJob;
                    if (bestJob?.defName.StartsWith("Rimbody_DoChunk") == true)
                    {
                        thing = Chunk;
                    }
                    else
                    {
                        thing = mattress;
                    }
                }
            }

            if (jobtogive != null)
            {
                if (jobtogive == DefOf_Rimbody.Rimbody_DoStrengthBuilding)
                {
                    Job job = DoTryGiveTargetJob(pawn, thing);
                    if (job != null)
                    {
                        return job;
                    }
                }
                else if (jobtogive.defName.StartsWith("Rimbody_DoChunk") == true)
                {
                    Job job = DoTryGiveChunkJob(pawn, thing, jobtogive);
                    if (job != null)
                    {
                        return job;
                    }
                }
                else
                {
                    if (workoutLocation != IntVec3.Invalid)
                    {
                        Job job = JobMaker.MakeJob(jobtogive, workoutLocation, thing);
                        return job;
                    }
                }
            }

            return null;
        }
        public static Job DoTryGiveTargetJob(Pawn pawn, Thing t)
        {
            RimbodyDefLists.ThingModExDB.TryGetValue(t.def.shortHash, out var targetModExtension);
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

        public static Job DoTryGiveChunkJob(Pawn pawn, Thing t, JobDef jobtogive)
        {
            if (pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
            {
                return JobMaker.MakeJob(jobtogive, t);
            }
            return null;
        }

        protected static void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (RimbodyDefLists.StrengthTargets == null || RimbodyDefLists.StrengthTargets.Count == 0) return;
            foreach (var buildingDef in RimbodyDefLists.StrengthTargets)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(buildingDef));
            }
        }
    }
}