using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public abstract class JobDriver_RimbodyBaseDriver : JobDriver
    {
        protected float joygainfactor = 1.0f;
        protected int tickProgress = 0;
        protected int workoutIndex = -1;
        protected float memoryFactor = 1.0f;
        protected float workoutEfficiencyValue = 1.0f;
        protected Vector3 pawnOffset = Vector3.zero;
        protected Rot4 lyingRotation = Rot4.Invalid;
        public override Vector3 ForcedBodyOffset
        {
            get
            {
                return pawnOffset;
            }
        }
        public override Rot4 ForcedLayingRotation
        {
            get
            {
                return lyingRotation;
            }
        }

        public abstract override bool TryMakePreToilReservations(bool errorOnFailed);
        protected abstract override IEnumerable<Toil> MakeNewToils();
        protected void GetInPosition(Thing building, Direction direction)
        {
            switch (direction)
            {
                case Direction.Center:
                    pawn.rotationTracker.FaceCell(building.Position);
                    break;
                case Direction.Away:
                    pawn.rotationTracker.FaceCell(2 * pawn.Position - building.Position);
                    break;
                case Direction.FaceSame:
                    pawn.Rotation = building.Rotation;
                    break;
                case Direction.FaceOpposite:
                    pawn.Rotation = building.Rotation.Opposite;
                    break;
                case Direction.LyingFrontSame:
                    pawn.SetPawnBodyAngleOverride(building.Rotation.Opposite.AsAngle);
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.LyingFrontOpposite:
                    pawn.SetPawnBodyAngleOverride(building.Rotation.AsAngle);
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.LyingDownSame:
                    pawn.SetPawnBodyAngleOverride(building.Rotation.Opposite.AsAngle);
                    pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                    lyingRotation = building.Rotation.Opposite == Rot4.South ? Rot4.North : building.Rotation.Opposite;
                    break;
                case Direction.LyingUpSame:
                    pawn.SetPawnBodyAngleOverride(building.Rotation.Opposite.AsAngle);
                    pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                    lyingRotation = building.Rotation == Rot4.North ? Rot4.South : building.Rotation;
                    break;
            }
        }
        protected static int GetWorkoutInt(CompPhysique compPhysique, ModExtensionRimbodyTarget ext, RimbodyWorkoutCategory category, out float memoryFactor)
        {
            float score = 0f;
            memoryFactor = 1f;
            int indexBest = -1;
            var numVarieties = ext.workouts.Count;
            if (numVarieties == 1)
            {
                if (category == RimbodyWorkoutCategory.Strength) memoryFactor = compPhysique.memory.Contains("strength|" + ext.workouts[0].name) ? 0.9f : 1f;
                else if (category == RimbodyWorkoutCategory.Balance) memoryFactor = compPhysique.memory.Contains("balance|" + ext.workouts[0].name) ? 0.9f : 1f;
                return 0;
            }
            for (int i = 0; i < numVarieties; i++)
            {
                if (ext.workouts[i].Category != category) continue;
                float tmpMemoryFactor = 1f;
                if (category == RimbodyWorkoutCategory.Strength) tmpMemoryFactor = compPhysique.memory.Contains("strength|" + ext.workouts[i].name) ? 0.9f : 1f;
                else if (category == RimbodyWorkoutCategory.Balance) tmpMemoryFactor = compPhysique.memory.Contains("balance|" + ext.workouts[i].name) ? 0.9f : 1f;
                float tmpScore = tmpMemoryFactor * compPhysique.GetWorkoutScore(category, ext.workouts[i]);
                if (tmpScore > score)
                {
                    score = tmpScore;
                    memoryFactor = tmpMemoryFactor;
                    indexBest = i;
                }
                else if (tmpScore == score)
                {
                    if (Rand.Chance(0.5f))
                    {
                        score = tmpScore;
                        memoryFactor = tmpMemoryFactor;
                        indexBest = i;
                    }
                }
            }
            return indexBest;
        }
        protected void AdjustJoygainFactor()
        {
            joygainfactor = TargetThingA.def.GetStatValueAbstract(StatDefOf.JoyGainFactor);
            var joyneed = pawn.needs?.joy;
            if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
            {
                joygainfactor = 0;
            }
        }
        protected void StartWorkout(CompPhysique compPhysique, WorkOut workout)
        {
            compPhysique.jobOverride = true;
            compPhysique.strengthOverride = workout.strength * workoutEfficiencyValue;
            compPhysique.cardioOverride = workout.cardio * workoutEfficiencyValue;
            compPhysique.memoryFactorOverride = memoryFactor;
            compPhysique.partsOverride = workout.strengthParts;
        }
        protected void FinishWorkout(CompPhysique compPhysique)
        {
            compPhysique.jobOverride = false;
            compPhysique.strengthOverride = 0f;
            compPhysique.cardioOverride = 0f;
            compPhysique.memoryFactorOverride = 1f;
            compPhysique.partsOverride = null;
            compPhysique.AssignedTick = Mathf.Max(0, compPhysique.AssignedTick - tickProgress);
        }
    }
}
