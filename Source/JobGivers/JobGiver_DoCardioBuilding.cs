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
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }

            float result = 5.5f;

            if (compPhysique.useFatgoal && compPhysique.FatGoal < compPhysique.BodyFat)
            {
                result += 2f + (compPhysique.BodyFat - compPhysique.FatGoal)/100f;
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
            //if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            //{
            //    return null;
            //}
            var compPhysique = pawn.TryGetComp<CompPhysique>();

            if (compPhysique == null)
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
                if (!pawn.CanReserve(t))
                {
                    return false;
                }
                if (t.IsForbidden(pawn))
                {
                    return false;
                }
                if (!t.IsSociallyProper(pawn))
                {
                    return false;
                }
                if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var chair))
                {
                    return false;
                }
                LocalTargetInfo target = result;
                if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, false))
                {
                    return false;
                }
                return !TooTired(pawn) && (t.TryGetComp<CompPowerTrader>()?.PowerOn ?? true);
            };

            float scoreFunc(Thing t)
            {
                string key = "cardio|" + t.def.defName;
                return compPhysique.memory.Contains(key) ? 1f : 2f;
            };

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
            if (!WatchBuildingUtility.TryFindBestWatchCell(t, pawn, false, out var result, out var chair))
            {
                return null;
            }
            LocalTargetInfo target = result;
            if (pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, false))
            {
                return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoCardioBuilding, t, result, chair);
            }
            return null;
        }

        protected virtual void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (RimbodyDefLists.CardioBuilding == null)
            {
                return;
            }
            if (RimbodyDefLists.CardioBuilding.Count == 1)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(RimbodyDefLists.CardioBuilding[0]));
                return;
            }
            for (int i = 0; i < RimbodyDefLists.CardioBuilding.Count; i++)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(RimbodyDefLists.CardioBuilding[i]));
            }
        }
    }
}
