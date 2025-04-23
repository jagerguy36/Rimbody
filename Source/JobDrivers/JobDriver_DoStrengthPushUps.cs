using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;
using UnityEngine;
using System;
using UnityEngine.Profiling;
using static System.Net.Mime.MediaTypeNames;
using static Verse.PawnRenderNodeProperties;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthPushUps : JobDriver
    {
        private const int duration = 800;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private Vector3 pawnNudge = Vector3.zero;
        private Rot4 lyingRotation = Rot4.Invalid;
        public override Vector3 ForcedBodyOffset
        {
            get
            {
                return pawnNudge;
            }
        }
        public override Rot4 ForcedLayingRotation
        {
            get
            {
                return lyingRotation;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }
        protected void WatchTickAction(Thing building)
        {
            if (joygainfactor > 0)
            {
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
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
            Scribe_Values.Look(ref tickProgress, "plank_tickProgress", 0);
            Scribe_Values.Look(ref pawnNudge, "plank_pawnNudget", Vector3.zero);
            Scribe_Values.Look(ref lyingRotation, "plank_lyingRotation", Rot4.Invalid);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            float jitter_amount = 3f * Mathf.Max(0f, (1f - (compPhysique.MuscleMass / 35f))) / 100f;
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            EndOnTired(this);

            var exWorkout = this.job.def.GetModExtension<ModExtensionRimbodyJob>();
            float score = compPhysique.GetStrengthPartScore(exWorkout.strengthParts, exWorkout.strength);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Rot4 facing = Rot4.Random;
            float adjsusted =(facing.Opposite.AsAngle > 0 && facing.Opposite.AsAngle < 180) ? -30f : (facing.Opposite.AsAngle > 180 && facing.Opposite.AsAngle < 360) ? 30f : 0f;

            float uptime = 0.75f - (20f * compPhysique.MuscleMass / 5000f);
            float cycleDuration = 125f - compPhysique.MuscleMass;

            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                pawn.pather.StopDead();
                pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                lyingRotation = facing.Opposite == Rot4.South ? Rot4.North : facing.Opposite;
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
                compPhysique.jobOverride = true;
                compPhysique.strengthOverride = exWorkout.strength;
                compPhysique.cardioOverride = exWorkout.cardio;
                compPhysique.durationOverride = duration;
                compPhysique.partsOverride = exWorkout.strengthParts;
            };
            workout.tickAction = delegate
            {
                tickProgress += 1;
                float nudgeMultiplier = 0f;
                float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                if (cycleTime < uptime)
                {
                    nudgeMultiplier = Mathf.Lerp(0f, 1f, cycleTime / uptime);
                }
                else
                {
                    nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - uptime) / (1f - uptime));
                }
                pawn.PawnBodyAngleOverride() = facing.Opposite.AsAngle + adjsusted*(1f + nudgeMultiplier)*0.5f;
                float xJitter = (Rand.RangeSeeded(-jitter_amount, jitter_amount, tickProgress));
                Vector3 JitterVector = IntVec3.West.RotatedBy(pawn.Rotation).ToVector3() * xJitter;
                pawnNudge = JitterVector + Vector3.forward*nudgeMultiplier*0.15f;
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
                compPhysique.durationOverride = 0;
                compPhysique.partsOverride = null;
                pawnNudge = Vector3.zero;
                lyingRotation = Rot4.Invalid;
                TryGainGymThought();
                AddMemory(compPhysique);
                pawn.PawnBodyAngleOverride() = -1;
            });
            yield return workout;
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
        private void TryGainGymThought()
        {
            var room = pawn.GetRoom();
            if (room == null)
            {
                return;
            }

            //get the impressive stage index for the current room
            var scoreStageIndex =
                RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness));
            //if the stage index exists in the definition (in xml), gain the memory (and buff)
            if (DefOf_Rimbody.WorkedOutInImpressiveGym.stages[scoreStageIndex] != null)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
                    ThoughtMaker.MakeThought(DefOf_Rimbody.WorkedOutInImpressiveGym,
                        scoreStageIndex));
            }
        }
    }
}
