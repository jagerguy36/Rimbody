//using RimWorld;
//using System.Collections.Generic;
//using System;
//using Verse;
//using Verse.AI;
//using System.Net;

//namespace Maux36.Rimbody
//{
//    internal class JobDriver_GoDoPushUps : JobDriver
//    {
//        public override bool TryMakePreToilReservations(bool errorOnFailed)
//        {
//            pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, job.targetA.Cell);
//            return true;
//        }
//        protected override IEnumerable<Toil> MakeNewToils()
//        {
//            LocalTargetInfo lookAtTarget = job.GetTarget(TargetIndex.B);
//            Toil toil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
//            this.FailOn(() => pawn.health.Downed);
//            this.FailOn(() => pawn.IsBurning());
//            this.FailOn(() => pawn.IsFighting());
//            this.FailOn(() => pawn.Drafted);
//            toil.FailOn(() => job.GetTarget(TargetIndex.A).Thing is Pawn pawn && pawn.ParentHolder is Corpse);
//            toil.FailOn(() => job.GetTarget(TargetIndex.A).Thing?.Destroyed ?? false);

//            Toil findPushupSpot = new Toil
//            {
//                initAction = delegate
//                {
//                    pawn.pather.StartPath(lookAtTarget, PathEndMode.OnCell);
//                },
//                defaultCompleteMode = ToilCompleteMode.PatherArrival
//            };
//            yield return findPushupSpot;

//            Toil toil2 = ToilMaker.MakeToil("MakeNewToils");
//            toil2.initAction = delegate
//            {
//                if (pawn.mindState != null && pawn.mindState.forcedGotoPosition == base.TargetA.Cell)
//                {
//                    pawn.mindState.forcedGotoPosition = IntVec3.Invalid;
//                }
//            };
//            toil2.defaultCompleteMode = ToilCompleteMode.Instant;
//            yield return toil2;
//        }
//    }
//}
