using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JobDriver_Jogging : JobDriver
    {
        private bool recorded = false;
        private bool jogger = false;
        private float joygainfactor = 1.0f;
        private int ticksLeft = 2500;
        
        private static readonly TraitDef SpeedOffsetDef = DefDatabase<TraitDef>.GetNamed("SpeedOffset", true);

        private static readonly IntRange WaitTicksRange = new IntRange(10, 50);
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            
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
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
            Scribe_Values.Look(ref recorded, "recorded", false);
            Scribe_Values.Look(ref jogger, "jogger", false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            var joyneed = pawn.needs?.joy;
            if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
            {
                joygainfactor = 0;
            }
            if (pawn?.story?.traits?.HasTrait(SpeedOffsetDef, 2) == true)
            {
                jogger = true;
            }
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.AddEndCondition(() => (ticksLeft <= 0 ? JobCondition.Succeeded : JobCondition.Ongoing));
            EndOnTired(this);
            Toil findInterestingThing = FindInterestingThing();
            yield return findInterestingThing;


            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            toil.AddPreTickAction(delegate
            {
                ticksLeft--;
                if (joygainfactor > 0)
                {
                    pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
                }
            });
            toil.AddFinishAction(delegate
            {
                AddMemory();
                if (ticksLeft < 5 && jogger)
                {
                    pawn?.needs?.mood?.thoughts?.memories?.TryGainMemory(DefOf_Rimbody.Rimbody_GoodRun);
                }
            });
            yield return toil;


            Toil wait = ToilMaker.MakeToil("MakeNewToils");
            
            wait.initAction = delegate
            {
                wait.actor.pather.StopDead();
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
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

        public static IJobEndable EndOnTired(IJobEndable f, JobCondition endCondition = JobCondition.InterruptForced)
        {
            Pawn actor = f.GetActor();
            bool isTired = TooTired(actor);
            f.AddEndCondition(() => (!isTired) ? JobCondition.Ongoing : endCondition);
            return f;
        }
        public static bool TooTired(Pawn actor)
        {
            if (((actor != null) & (actor.needs != null)) && actor.needs.rest != null && (double)actor.needs.rest.CurLevel < 0.17f)
            {
                return true;
            }
            return false;
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
                if (r.Room != null) // Check if it's an indoor room
                {
                    if (!r.Room.PsychologicallyOutdoors && r.Room.CellCount <= 400) // Ensure room size is larger than 400
                    {
                        return false;
                    }
                }
                if (r.TryFindRandomCellInRegion(CellValidator, out var result) && (!bfsTarget.IsValid || searcher.Position.DistanceTo(bfsTarget.Cell) != searcher.Position.DistanceTo(result)))
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
                    //return terrain.natural;
                    return true;
                }
                return false;
            }
        }
    }
}