using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public class StrengthEnterable : Building_Enterable, IThingHolderWithDrawnPawn, IThingHolder
    {
        private int ticksRemaining;

        [Unsaved(false)]
        private Texture2D cachedInsertPawnTex;

        private const int TicksWorkout = 1000;

        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        private Pawn ContainedPawn => innerContainer.FirstOrDefault() as Pawn;

        public override bool IsContentsSuspended => false;

        public Texture2D InsertPawnTex
        {
            get
            {
                if (cachedInsertPawnTex == null)
                {
                    cachedInsertPawnTex = ContentFinder<Texture2D>.Get("UI/Gizmos/InsertPawn");
                }
                return cachedInsertPawnTex;
            }
        }

        public float HeldPawnDrawPos_Y => DrawPos.y + 3f / 74f;

        public float HeldPawnBodyAngle => base.Rotation.Opposite.AsAngle;
        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public override Vector3 PawnDrawOffset => new Vector3(0, 0, -0.2f);// IntVec3.West.RotatedBy(base.Rotation).ToVector3() / def.size.x; //south: (0, 0, -0.2f);, north: (0, 0, 0.2f); east: (0.2f, 0, 0); west: (-0.2f, 0, 0);

        public override void Tick()
        {
            base.Tick();
            innerContainer.ThingOwnerTick();
            if (base.Working)
            {
                if (ContainedPawn == null)
                {
                    Cancel();
                    return;
                }
                TickEffects();
                ticksRemaining--;
                if (ticksRemaining <= 0)
                {
                    Finish();
                }
                return;
            }
            else
            {
                if (selectedPawn != null && selectedPawn.Dead)
                {
                    Cancel();
                }
            }
        }

        private void TickEffects()
        {
            //if (mote != null)
            //{
            //    mote.progress = 1f - Mathf.Clamp01((float)ticksRemaining / 30000f);
            //    mote.offsetZ = ((base.Rotation == Rot4.North) ? 0.5f : (-0.5f));
            //}
        }

        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony)
            {
                return false;
            }
            if (selectedPawn != null && selectedPawn != pawn)
            {
                return false;
            }
            if (!pawn.RaceProps.Humanlike || pawn.IsQuestLodger())
            {
                return false;
            }
            if (innerContainer.Count > 0)
            {
                return "Occupied".Translate();
            }
            return true;
        }

        private void Cancel()
        {
            startTick = -1;
            selectedPawn = null;
            innerContainer.TryDropAll(def.hasInteractionCell ? InteractionCell : base.Position, base.Map, ThingPlaceMode.Near);
        }

        private void Finish()
        {
            Log.Message("finished");
            startTick = -1;
            selectedPawn = null;
            if (ContainedPawn == null)
            {
                return;
            }
            Pawn containedPawn = ContainedPawn;
            var compPhysique = containedPawn.TryGetComp<CompPhysique>();
            AddMemory(compPhysique, "testDef");
            IntVec3 intVec = (def.hasInteractionCell ? InteractionCell : base.Position);
            innerContainer.TryDropAll(intVec, base.Map, ThingPlaceMode.Near);
            TryGainGymThought(containedPawn);
        }
        private void AddMemory(CompPhysique compPhysique, string name)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"strength|{name}");
            }
        }

        public static bool TooTired(Pawn actor)
        {
            if (((actor != null) & (actor.needs != null)) && actor.needs.rest != null && (double)actor.needs.rest.CurLevel < 0.17f)
            {
                return true;
            }
            return false;
        }
        private void TryGainGymThought(Pawn pawn)
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

        public override void TryAcceptPawn(Pawn pawn)
        {
            if ((bool)CanAcceptPawn(pawn))
            {
                selectedPawn = pawn;
                bool num = pawn.DeSpawnOrDeselect();
                if (innerContainer.TryAddOrTransfer(pawn))
                {
                    startTick = Find.TickManager.TicksGame;
                    ticksRemaining = 1000;

                    Log.Message($"Set start tick {startTick} and ticksremaining. {ticksRemaining}");
                }
                if (num)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
            }
        }

        //protected override void SelectPawn(Pawn pawn)
        //{
        //    base.SelectPawn(pawn);
        //}

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (base.Working)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "Stop Working out".Translate();
                command_Action.defaultDesc = "Stop Working out".Translate();
                command_Action.icon = CancelIcon;
                command_Action.action = delegate
                {
                    Cancel();
                };
                command_Action.activateSound = SoundDefOf.Designate_Cancel;
                yield return command_Action;
                yield break;
            }
        }
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            if (base.Working && ContainedPawn != null)
            {
                ContainedPawn.Drawer.renderer.DynamicDrawPhaseAt(phase, drawLoc + PawnDrawOffset, null, neverAimWeapon: true);
            }
        }

        //public override string GetInspectString()
        //{
        //    string text = base.GetInspectString();
        //    if (base.Working && ContainedPawn != null)
        //    {
        //        if (!text.NullOrEmpty())
        //        {
        //            text += "\n";
        //        }
        //        text = text + "ExtractingXenogermFrom".Translate(ContainedPawn.Named("PAWN")).Resolve() + "\n";
        //    }
        //    return text;
        //}

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
        }
    }
}
