using RimWorld;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class DeadliftTickhandler : IWorkoutTickHandler
    {
        public void TickAction(Pawn pawn, Building_WorkoutAnimated building, WorkOut wo, float uptime, float cycleDuration, float jitter_amount, int tickProgress, ref Vector3 pawnOffset, ref Rot4 lyingRotation)
        {
            cycleDuration += 50f;
            float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
            float nudgeMultiplier;
            Vector3 buildingOffset = Vector3.zero;

            float animBase = 0f;
            float animCoef = 0f;

            if (building.Rotation.AsAngle > 0 && building.Rotation.AsAngle < 180)
            {
                animBase = 35f;
                animCoef = -35f;
                pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                lyingRotation = building.Rotation;
            }
            else if (building.Rotation.AsAngle > 180 && building.Rotation.AsAngle < 360)
            {
                animBase = 325f;
                animCoef = 35f;
                pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                lyingRotation = building.Rotation;
            }


            if (cycleTime < 0.4f)
            {
                nudgeMultiplier = 0f;
                jitter_amount = 0f;
            }
            if (cycleTime < 0.55f) nudgeMultiplier = Mathf.Lerp(0f, 1f, (cycleTime - 0.4f) / 0.15f);
            else if (cycleTime < 0.95f) nudgeMultiplier = 1f;
            else if (cycleTime < 1f) nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - 0.95f) / 0.05f);
            else
            {
                nudgeMultiplier = 0f;
                jitter_amount = 0f;
            }

            //Pawn
            if (wo?.pawnAnimOffset?.FromRot(building.Rotation) != null)
            {
                pawnOffset = wo.pawnAnimOffset.FromRot(building.Rotation);
            }
            if (wo?.pawnAnimPeak?.FromRot(pawn.Rotation) != null && wo?.pawnAnimPeak?.FromRot(pawn.Rotation) != Vector3.zero)
            {
                pawnOffset += nudgeMultiplier * wo.pawnAnimPeak.FromRot(pawn.Rotation) + IntVec3.West.RotatedBy(pawn.Rotation).ToVector3() * Rand.Range(-jitter_amount, jitter_amount);
            }
            pawn.SetPawnBodyAngleOverride(animBase + (animCoef * nudgeMultiplier));
            //Building
            if (wo?.movingpartAnimOffset?.FromRot(building.Rotation) != null)
            {
                buildingOffset = wo.movingpartAnimOffset.FromRot(building.Rotation);
            }
            if (wo?.movingpartAnimPeak?.FromRot(building.Rotation) != null)
            {
                buildingOffset += nudgeMultiplier * wo.movingpartAnimPeak.FromRot(building.Rotation) + IntVec3.West.RotatedBy(building.Rotation).ToVector3() * Rand.Range(-jitter_amount, jitter_amount);
            }
            building.calculatedOffset = buildingOffset;
        }
    }
}
