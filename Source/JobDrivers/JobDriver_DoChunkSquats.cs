using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoChunkSquats : JobDriver
    {
        private const int duration = 800;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private float memoryFactor = 1.0f;
        private Vector3 itemOffset = new Vector3(0f, -1f / 26f, 0.45f);
        private Vector3 pawnNudge = Vector3.zero;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed))
            {
                return false;
            }
            return true;
        }
        public override Vector3 ForcedBodyOffset
        {
            get
            {
                return pawnNudge;
            }
        }

        private void AddMemory(CompPhysique compPhysique)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"strength|{job.def.defName}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "chunksquats_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "chunksquatss_tickProgress", 0);
            Scribe_Values.Look(ref memoryFactor, "chunksquats_memoryFactor", 1.0f);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax * 0.95f) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = 1;
            });
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_General.DoAtomic(delegate
            {
                pawn.carryTracker.TryStartCarry(TargetA.Thing, 1);
            });

            var exWorkout = this.job.def.GetModExtension<ModExtensionRimbodyJob>();
            memoryFactor = compPhysique.memory.Contains("strength|" + job.def.defName) ? 0.9f : 1f;

            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceCell(pawn.Position + new IntVec3(0, 0, -1));
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
                compPhysique.jobOverride = true;
                compPhysique.strengthOverride = exWorkout.strength;
                compPhysique.cardioOverride = exWorkout.cardio;
                compPhysique.memoryFactorOverride = memoryFactor;
                compPhysique.partsOverride = exWorkout.strengthParts;
            };
            float uptime = 0.95f - (0.004f * compPhysique.MuscleMass);
            float cycleDuration = 125f - compPhysique.MuscleMass;
            float jiggle_amount = 0.03f * (1f - (compPhysique.MuscleMass / 50f));
            float nudgeMultiplier;
            workout.tickAction = delegate
            {
                tickProgress++;
                float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                if (cycleTime < uptime)
                {
                    nudgeMultiplier = Mathf.Lerp(0f, 1f, cycleTime / uptime);
                }
                else
                {
                    nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - uptime) / (1f - uptime));
                }
                pawnNudge.x = Rand.Range(-jiggle_amount, jiggle_amount);
                pawnNudge.z = nudgeMultiplier * 0.13f - 0.1f;
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                compPhysique.jobOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                compPhysique.memoryFactorOverride = 1f;
                compPhysique.partsOverride = null;
                AddMemory(compPhysique);
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            });
            yield return workout;
        }

        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
        {
            if (tickProgress > 0)
            {
                drawPos += itemOffset;
                return true;
            }
            return false;
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
    }
}
