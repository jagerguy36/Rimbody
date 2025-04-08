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
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }

            float result = 5.5f;

            if (compPhysique.useMuscleGoal && compPhysique.MuscleGoal > compPhysique.MuscleMass)
            {
                result += 2.5f + ((compPhysique.MuscleGoal - compPhysique.MuscleMass)/100f);
            }
            else
            {
                result += (25f - compPhysique.MuscleMass) / 100f;
            }

            if (compPhysique.gain >= compPhysique.gainMax)
            {
                result = 0f;
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
            tmpCandidates.Clear();
            GetSearchSet(pawn, tmpCandidates);
            if (tmpCandidates.Count == 0)
            {
                return null;
            }

            Predicate<Thing> predicate = delegate (Thing t)
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
                    if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var chair))
                    {
                        return false;
                    }
                    LocalTargetInfo target = result;
                    if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
                    {
                        return false;
                    }
                    return t.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;
                }
                else if(targetModExtension.Type == RimbodyTargetType.Container)
                {
                    Log.Message($"container called {t.def.defName}");
                    if (!(t is Building_Enterable building_Enterable))
                    {
                        Log.Message($"{t.def.defName} got 1");
                        return false;
                    }
                    if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null))
                    {
                        Log.Message($"{t.def.defName} got 2");
                        return false;
                    }
                    if (t.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
                    {
                        Log.Message($"{t.def.defName} got 3");
                        return false;
                    }
                    if (!building_Enterable.CanAcceptPawn(pawn))
                    {
                        Log.Message($"{t.def.defName} got 5");
                        return false;
                    }

                    Log.Message($"{t.def.defName} got through");
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

            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (compPhysique == null)
            {
                return null;
            }
            float scoreFunc(Thing t)
            {
                if(RimbodyDefLists.StrengthTarget.TryGetValue(t.def, out var targetModExtension))
                {
                    float score = 0f;
                    foreach (WorkOut workout in targetModExtension.workouts)
                    {
                        score = Math.Max(score, compPhysique.GetScore(RimbodyTargetCategory.Strength, workout));
                    }
                    return score;
                }
                return 0;
            }

            Thing thing = null;
            thing ??= GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 9999f, predicate, scoreFunc);
            tmpCandidates.Clear();

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
            RimbodyDefLists.StrengthTarget.TryGetValue(t.def, out var targetModExtension);
            if (targetModExtension.Type == RimbodyTargetType.Building)
            {
                if (targetModExtension.buildingUsecell)
                {
                    if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var chair))
                    {
                        return null;
                    }
                    LocalTargetInfo target = result;
                    if (pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
                    {
                        return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoStrengthBuilding, t, result, chair);
                    }
                }
            }
            else if (targetModExtension.Type == RimbodyTargetType.Container)
            {
                if (!(t is Building_Enterable))
                {
                    return null;
                }
                return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_EnterWorkoutBuilding, t);
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