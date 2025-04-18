﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    internal class JobGiver_DoBalanceBuilding : ThinkNode_JobGiver
    {
        private static List<Thing> tmpCandidates = [];
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }

            float result = 4.0f;

            if(compPhysique.memory?.Count > 0)
            {
                foreach (var item in compPhysique.memory)
                {
                    int delimiterIndex = item.IndexOf('|');
                    if (delimiterIndex >= 0)
                    {
                        string firstPart = item.Substring(0, delimiterIndex);
                        if (firstPart == "balance")
                        {
                            result -= 0.8f;
                        }
                        else
                        {
                            result += 0.4f;
                        }
                    }
                }
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
                RimbodyDefLists.BalanceTarget.TryGetValue(t.def, out var targetModExtension);
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
                    return false;
                    //if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some))
                    //{
                    //    return false;
                    //}
                    //return true;
                }
            };
            float scoreFunc(Thing t)
            {
                if (RimbodyDefLists.BalanceTarget.TryGetValue(t.def, out var targetModExtension))
                {
                    float score = 0f;
                    foreach (WorkOut workout in targetModExtension.workouts)
                    {
                        score = Math.Max(score, compPhysique.GetScore(RimbodyTargetCategory.Balance, workout));
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
            RimbodyDefLists.BalanceTarget.TryGetValue(t.def, out var targetModExtension);
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
                        return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoBalanceBuilding, t, result, chair);
                    }
                }
                return null;
            }
            else
            {
                return null;//balance item not yet implemented.
                //if (pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some))
                //{
                //    return null;
                //}
            }
        }

        protected virtual void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (RimbodyDefLists.BalanceTarget == null || RimbodyDefLists.BalanceTarget.Count == 0)
            {
                return;
            }
            foreach (var buildingDef in RimbodyDefLists.BalanceTarget.Keys)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(buildingDef));
            }
        }
    }
}
