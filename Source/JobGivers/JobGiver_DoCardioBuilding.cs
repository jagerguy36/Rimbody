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
            List<Thing> tmpCandidates = [];
            List<Thing> freshCandidates = [];
            if (pawn.Downed || pawn.Drafted)
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
            GetSearchSet(pawn, tmpCandidates);
            if (tmpCandidates.Count == 0)
            {
                return null;
            }

            freshCandidates = tmpCandidates.Where(thing => !compPhysique.memory.Contains("cardio|" + thing.def.defName)).ToList();

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
                return !TooTired(pawn) && (t.TryGetComp<CompPowerTrader>()?.PowerOn ?? true);
            };
            Predicate<Thing> predicate2 = predicate;
            Thing thing = null;
            if (freshCandidates.Count != 0)
            {
                thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, freshCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 25f, predicate2);
                freshCandidates.Clear();
            }

            thing ??= GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, tmpCandidates, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Some), 9999f, predicate2);
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
            return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoCardioBuilding, t, result, chair);
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
