using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthBuilding : JobDriver
    {
        private bool faceaway = false;
        private bool isMetal = false;
        private float joygainfactor = 1.0f;

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

            return true;
        }
        protected void GetInPosition(Thing building)
        {
            pawn.rotationTracker.FaceCell(base.TargetA.Cell);

            if (faceaway)
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
            if (pawn.IsHashIntervalTick(50 + Rand.Range(0, 10)))
            {
                if (isMetal)
                {
                    RimWorld.SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                }
                pawn.Drawer.Notify_MeleeAttackOn(building);
            }
            if (joygainfactor > 0)
            {
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
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
                var ext = TargetThingA.def.GetModExtension<ModExtentionRimbodyBuilding>();
                if (ext != null)
                {
                    faceaway = ext.faceaway;
                    isMetal = ext.isMetal;
                }
                joygainfactor = TargetThingA.def.GetStatValueAbstract(StatDefOf.JoyGainFactor);
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
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
