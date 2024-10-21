﻿using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoCardioBuilding : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed))
            {
                return false;
            }
            if (!pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed))
            {
                return false;
            }

            AddEndCondition(() =>
            {
                if (pawn?.timetable?.CurrentAssignment != DefOf_Rimbody.Rimbody_Workout)
                    return JobCondition.Succeeded;
                return JobCondition.Ongoing;
            });

            return true;
        }
        protected void GetInPosition(Thing building)
        {
            pawn.rotationTracker.FaceCell(base.TargetA.Cell);
            var ext = building.def.GetModExtension<ModExtentionRimbodyBuilding>();
            if (ext != null && ext.faceaway)
            {
                FieldInfo fieldInfo = typeof(Thing).GetField("rotationInt", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    Rot4 rotationValue = (Rot4)fieldInfo.GetValue(building);
                    pawn.Rotation = rotationValue;
                }
            }
        }
        protected void WatchTickAction(Thing building)
        {
            //var ext = buildingdef.GetModExtension<ModExtentionRimbodyBuilding>();
            //if (ext != null && ext.isMetal)
            //{
            //    //if (pawn.IsHashIntervalTick(800 + Rand.Range(0, 200)))
            //    //{
            //    //    RimWorld.SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
            //    //}
            //}

            pawn.needs?.joy?.GainJoy(1.0f * building.def.GetStatValueAbstract(StatDefOf.JoyGainFactor) * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
        }

        private void AddMemory(ThingDef buildingdef)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"cardio|{buildingdef.defName}");
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            EndOnTired(this);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                GetInPosition(TargetThingA);
            };
            workout.AddPreTickAction(delegate
            {
                WatchTickAction(TargetThingA);
            });
            workout.AddFinishAction(delegate
            {
                TryGainGymThought();
                AddMemory(TargetThingA.def);
            });
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = 2500;
            workout.handlingFacing = true;
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
                pawn.needs.mood.thoughts.memories.TryGainMemory(
                    ThoughtMaker.MakeThought(DefOf_Rimbody.WorkedOutInImpressiveGym,
                        scoreStageIndex));
            }
        }
    }
}
