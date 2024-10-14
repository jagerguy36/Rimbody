﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Verse.Sound;
using UnityEngine.Profiling;
using System.Reflection;
using static UnityEngine.GraphicsBuffer;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthBuilding : JobDriver
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
            var ext = building.def.GetModExtension<ModExtentionRimbodyBuilding>();
            if (pawn.IsHashIntervalTick(50 + Rand.Range(0, 10)))
            {
                if (ext != null && ext.isMetal)
                {
                    RimWorld.SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                }
                pawn.Drawer.Notify_MeleeAttackOn(building);

            }
        }

        private void AddMemory(ThingDef buildingdef)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"strength|{buildingdef.defName}");
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
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = 1000;
            workout.AddFinishAction(delegate
            {
                TryGainGymThought();
                AddMemory(TargetThingA.def);
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
                pawn.needs.mood.thoughts.memories.TryGainMemory(
                    ThoughtMaker.MakeThought(DefOf_Rimbody.WorkedOutInImpressiveGym,
                        scoreStageIndex));
            }
        }
    }
}
