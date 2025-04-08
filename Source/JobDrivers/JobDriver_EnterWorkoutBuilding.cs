using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;
using UnityEngine;
using System;

namespace Maux36.Rimbody
{
    internal class JobDriver_EnterWorkoutBuilding : JobDriver
    {
        private Building_Enterable Building => (Building_Enterable)job.targetA.Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Building.CanAcceptPawn(pawn));
            yield return Toils_General.Do(delegate
            {
                Building.SelectedPawn = pawn;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Do(delegate
            {
                Building.TryAcceptPawn(pawn);
            });
        }
    }
}
