using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;
using Unity.Jobs;

namespace Maux36.Rimbody
{
    internal class JobGiver_DoStrengthBuilding : ThinkNode_JobGiver
    {
        private static List<Thing> tmpCandidates = [];
        private static Dictionary<ThingDef, float> workoutCache = new Dictionary<ThingDef, float>();
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }
            if (compPhysique.gain >= compPhysique.gainMax*0.95f)
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
            if (pawn.Downed || pawn.Drafted) return null;
            if (TooTired(pawn)) return null;
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return null;

            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (compPhysique == null) return null;

            tmpCandidates.Clear();
            workoutCache.Clear();
            GetSearchSet(pawn, tmpCandidates);
            Predicate<Thing> targetPredicate = delegate (Thing t)
            {
                if (t.IsForbidden(pawn)) return false;

                RimbodyDefLists.StrengthTarget.TryGetValue(t.def, out var targetModExtension);//TODO: null check?
                if (targetModExtension.Type == RimbodyTargetType.Building)
                {
                    if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) return false;
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
                if(RimbodyDefLists.StrengthTarget.TryGetValue(t.def, out var targetModExtension))
                {
                    float score = 0f;
                    if (workoutCache.ContainsKey(t.def))
                    {
                        score = workoutCache[t.def];
                        if (score > targethighscore)
                        {
                            targethighscore = score;
                        }
                        return score;
                    }
                    foreach (WorkOut workout in targetModExtension.workouts)
                    {
                        float tmpScore = (compPhysique.memory.Contains("strength|" + workout.name) ? 0.9f : 1f) * compPhysique.GetWorkoutScore(RimbodyTargetCategory.Strength, workout);
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
            JobDef jobtogive = null;
            thing ??= GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 9999f, targetPredicate, scoreFunc);
            if(thing != null)
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
                    if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some)) return false;
                    if (t.IsForbidden(pawn)) return false;
                    if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Haul) != null) return false;
                    return true;
                }
                Thing Chunk = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Chunk), PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 20f, chunkPredicate);

                foreach (var (strengthJobdef, strengthEx) in RimbodyDefLists.StrengthNonTargetJob)
                {
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
                if (maxScore > targethighscore)
                {
                    jobtogive = bestJob;
                    if (bestJob?.defName.StartsWith("Rimbody_DoChunk")==true)
                    {
                        thing = Chunk;
                    }
                }
            }


            if(jobtogive != null)
            {
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
                    else if (jobtogive.defName.StartsWith("Rimbody_DoChunk") == true)
                    {
                        Job job = DoTryGiveChunkJob(pawn, thing, jobtogive);
                        if (job != null)
                        {
                            return job;
                        }
                    }
                }
                else
                {
                    if (CellFinder.TryFindRandomReachableNearbyCell(pawn.Position, pawn.Map, 5, TraverseParms.For(pawn), (IntVec3 x) => x.Standable(pawn.Map), (Region x) => true, out IntVec3 workoutLocation))
                    {
                        Job job = JobMaker.MakeJob(jobtogive, workoutLocation);
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