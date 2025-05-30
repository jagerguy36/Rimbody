﻿using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public interface IWorkoutTickHandler
    {
        void TickAction(
        Pawn pawn,
        Building_WorkoutAnimated building,
        WorkOut workout,
        float uptime,
        float cycleDuration,
        float jitterAmount,
        int tickProgress,
        ref Vector3 pawnOffset,
        ref Rot4 lyingRotation);
    }
}
