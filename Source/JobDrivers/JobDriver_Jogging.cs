using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JobDriver_Jogging : JobDriver
    {
        private bool recorded = false;
        private int ticksLeft = 1500;

        private static readonly IntRange WaitTicksRange = new IntRange(10, 50);
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            AddEndCondition(() =>
            {
                if (pawn?.timetable?.CurrentAssignment != DefOf_Rimbody.Rimbody_Workout)
                    return JobCondition.Succeeded;
                if (ticksLeft <= 0)
                    return JobCondition.Succeeded;
                return JobCondition.Ongoing;
            });
            return true;
        }

        private Toil FindInterestingThing()
        {
            Toil toil = ToilMaker.MakeToil("FindInterestingThing");
            toil.initAction = delegate
            {
                if (TryFindNatureJoggingTarget(pawn, out var interestTarget))
                {
                    job.SetTarget(TargetIndex.A, interestTarget);
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private void AddMemory()
        {
            if (!recorded) {
                var compPhysique = pawn.TryGetComp<CompPhysique>();
                if (compPhysique != null)
                {
                    compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                    compPhysique.AddNewMemory("cardio|jogging");
                    recorded = true;
                }
                Log.Message("Memory Added");
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Toil findInterestingThing = FindInterestingThing();
            yield return findInterestingThing;


            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            toil.AddPreTickAction(delegate
            {
                ticksLeft--;
            });
            toil.AddFinishAction(delegate
            {
                AddMemory();
            });
            yield return toil;


            Toil wait = ToilMaker.MakeToil("MakeNewToils");
            
            wait.initAction = delegate
            {
                wait.actor.pather.StopDead();
            };
            wait.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(base.TargetA);
            };
            wait.defaultCompleteMode = ToilCompleteMode.Delay;
            wait.defaultDuration = WaitTicksRange.RandomInRange;
            wait.handlingFacing = true;
            yield return wait;
            yield return Toils_Jump.Jump(findInterestingThing);
        }


        //Code from the vanilla nature running utility.
        private static Dictionary<Region, bool> tmpRegionContainsOwnedBuildingCache_jogging = new();
        public static bool TryFindNatureJoggingTarget(Pawn searcher, out LocalTargetInfo interestTarget)
        {
            interestTarget = LocalTargetInfo.Invalid;
            if (!JoyUtility.EnjoyableOutsideNow(searcher))
            {
                return false;
            }
            tmpRegionContainsOwnedBuildingCache_jogging.Clear();
            LocalTargetInfo bfsTarget = LocalTargetInfo.Invalid;
            TraverseParms traverseParms = TraverseParms.For(searcher);
            int additionalRegionsToCheck = 8;
            RegionTraverser.BreadthFirstTraverse(searcher.Position, searcher.Map, (Region from, Region r) => r.Allows(traverseParms, isDestination: false), delegate (Region r)
            {
                if (bfsTarget.IsValid && --additionalRegionsToCheck <= 0)
                {
                    return true;
                }
                if (r.IsForbiddenEntirely(searcher))
                {
                    return false;
                }
                if (r.DangerFor(searcher) == Danger.Deadly)
                {
                    return false;
                }
                if (r.extentsClose.ClosestDistSquaredTo(searcher.Position) < 225)
                {
                    return false;
                }
                CellRect checkNearbyRegionsTouchingArea = r.extentsClose.ExpandedBy(15);
                bool foundNearbyRegionWithBuildings = false;
                RegionTraverser.BreadthFirstTraverse(r, (Region from, Region to) => to.extentsClose.Overlaps(checkNearbyRegionsTouchingArea), delegate (Region region)
                {
                    if (tmpRegionContainsOwnedBuildingCache_jogging.TryGetValue(region, out var value))
                    {
                        if (value)
                        {
                            foundNearbyRegionWithBuildings = true;
                            return true;
                        }
                        return false;
                    }
                    foreach (Thing item in region.ListerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
                    {
                        if (item.Faction != null)
                        {
                            foundNearbyRegionWithBuildings = true;
                            tmpRegionContainsOwnedBuildingCache_jogging[region] = true;
                            return true;
                        }
                    }
                    tmpRegionContainsOwnedBuildingCache_jogging[region] = false;
                    return false;
                });
                if (foundNearbyRegionWithBuildings)
                {
                    return false;
                }
                if (r.TryFindRandomCellInRegion(CellValidator, out var result) && (!bfsTarget.IsValid || searcher.Position.DistanceTo(bfsTarget.Cell) > searcher.Position.DistanceTo(result)))
                {
                    bfsTarget = result;
                }
                return false;
            }, 300);
            tmpRegionContainsOwnedBuildingCache_jogging.Clear();
            interestTarget = bfsTarget;
            return interestTarget.IsValid;
            bool CellValidator(IntVec3 c)
            {
                TerrainDef terrain = c.GetTerrain(searcher.Map);
                if (!terrain.avoidWander)
                {
                    return terrain.natural;
                }
                return false;
            }
        }
    }
}