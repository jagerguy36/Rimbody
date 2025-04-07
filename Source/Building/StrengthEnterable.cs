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

        public override Vector3 PawnDrawOffset => IntVec3.West.RotatedBy(base.Rotation).ToVector3() / def.size.x;

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
            startTick = -1;
            selectedPawn = null;
            if (ContainedPawn == null)
            {
                return;
            }
            Pawn containedPawn = ContainedPawn;
            IntVec3 intVec = (def.hasInteractionCell ? InteractionCell : base.Position);
            innerContainer.TryDropAll(intVec, base.Map, ThingPlaceMode.Near);
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
                }
                if (num)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
            }
        }

        protected override void SelectPawn(Pawn pawn)
        {
            base.SelectPawn(pawn);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (base.Working)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CommandCancelExtraction".Translate();
                command_Action.defaultDesc = "CommandCancelExtractionDesc".Translate();
                command_Action.icon = CancelIcon;
                command_Action.action = delegate
                {
                    Cancel();
                };
                command_Action.activateSound = SoundDefOf.Designate_Cancel;
                yield return command_Action;
                if (DebugSettings.ShowDevGizmos)
                {
                    Command_Action command_Action2 = new Command_Action();
                    command_Action2.defaultLabel = "DEV: Finish extraction";
                    command_Action2.action = delegate
                    {
                        Finish();
                    };
                    yield return command_Action2;
                }
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

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (base.Working && ContainedPawn != null)
            {
                if (!text.NullOrEmpty())
                {
                    text += "\n";
                }
                text = text + "ExtractingXenogermFrom".Translate(ContainedPawn.Named("PAWN")).Resolve() + "\n";
            }
            return text;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
        }
    }
}
