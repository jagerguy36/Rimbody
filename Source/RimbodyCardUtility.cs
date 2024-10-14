using RimWorld;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;
using Verse.Sound;
using System;

namespace Maux36.Rimbody
{
    public static class RimbodyCardUtility
    {
        public enum ScrollDirection
        {
            Up,
            Down
        }

        public static bool editMode;

        public static void DrawRimbodyCard(Rect rect, Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (pawn == null || compPhysique == null)
            {
                return;
            }

            Widgets.ThingIcon(new Rect(0f, 10f, 40f, 40f), pawn);
            Text.Font = GameFont.Medium;
            var rect2 = new Rect(56f, 0f, rect.width, 30f);
            Widgets.Label(rect2, pawn.Name.ToStringShort);
            Text.Font = GameFont.Tiny;
            var num = rect2.y + rect2.height;
            var rect3 = new Rect(56f, num, rect.width, 24f);
            Widgets.Label(rect3, "RimbodyWindow".Translate());
            Text.Font = GameFont.Small;
            num += rect3.height + 17f;

            var rect6 = new Rect(0f, num, rect.width - 10f, 24f);
            Widgets.Label(new Rect(10f, num, rect.width, 24f),
                "BodyFat".Translate() + ": " + Math.Round(compPhysique.BodyFat, 2));
            TipSignal tip3 = "FatTooltip".Translate();
            TooltipHandler.TipRegion(rect6, tip3);
            Widgets.DrawHighlightIfMouseover(rect6);
            num += rect6.height + 2f;
            var rect7 = new Rect(0f, num, rect.width - 10f, 24f);
            Widgets.Label(new Rect(10f, num, rect.width, 24f),
                "MuscleMass".Translate() + ": " + Math.Round(compPhysique.MuscleMass, 2));
            TipSignal tip4 = "MuscleTooltip".Translate();
            TooltipHandler.TipRegion(rect7, tip4);
            Widgets.DrawHighlightIfMouseover(rect7);

            num += rect7.height + 7f;

            var rect10 = new Rect(10f, num, rect.width, 24f);
            if (editMode)
            {
                GUI.color = Color.red;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect10, "Rimbody_EditModeTooltip".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            num += rect10.height + 5f;
            var rect11 = new Rect((rect.width / 2f) - 90f, num, 180f, 40f);
            if (Event.current.type == EventType.KeyDown)
            {
                _ = Event.current.keyCode == KeyCode.Mouse0;
            }
            else
            {
                _ = 0;
            }

            if (Event.current.type == EventType.KeyDown)
            {
                _ = Event.current.keyCode == KeyCode.Mouse1;
            }
            else
            {
                _ = 0;
            }

            if (!editMode)
            {
                if (!Widgets.ButtonText(rect11, "Rimbody_EditModeOn".Translate()))
                {
                    return;
                }

                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                editMode = true;

                return;
            }

            if (Widgets.ButtonText(rect11, "Rimbody_EditModeOff".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                editMode = false;
            }

            else if (ScrolledDown(rect6, true) || LeftClicked(rect6))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                compPhysique.BodyFat += 1;
                if (compPhysique.BodyFat > 50)
                {
                    compPhysique.BodyFat = 0;
                }
                compPhysique.ResetBody(pawn);
            }
            else if (ScrolledUp(rect6, true) || RightClicked(rect6))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                compPhysique.BodyFat -= 1;
                if (compPhysique.BodyFat < 0)
                {
                    compPhysique.BodyFat = 50;
                }
                compPhysique.ResetBody(pawn);
            }
            else if (ScrolledDown(rect7, true) || LeftClicked(rect7))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                compPhysique.MuscleMass += 1;
                if (compPhysique.MuscleMass > 50)
                {
                    compPhysique.MuscleMass = 0;
                }
                compPhysique.ResetBody(pawn);
            }
            else if (ScrolledUp(rect7, true) || RightClicked(rect7))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                compPhysique.MuscleMass -= 1;
                if (compPhysique.MuscleMass < 0)
                {
                    compPhysique.MuscleMass = 50;
                }
                compPhysique.ResetBody(pawn);
            }
        }

        public static bool Scrolled(Rect rect, ScrollDirection direction, bool stopPropagation)
        {
            var num = Event.current.type == EventType.ScrollWheel &&
                      (Event.current.delta.y > 0f && direction == ScrollDirection.Up ||
                       Event.current.delta.y < 0f && direction == ScrollDirection.Down) && Mouse.IsOver(rect);
            if (num && stopPropagation)
            {
                Event.current.Use();
            }

            return num;
        }

        public static bool ScrolledUp(Rect rect, bool stopPropagation = false)
        {
            return Scrolled(rect, ScrollDirection.Up, stopPropagation);
        }

        public static bool ScrolledDown(Rect rect, bool stopPropagation = false)
        {
            return Scrolled(rect, ScrollDirection.Down, stopPropagation);
        }

        public static bool Clicked(Rect rect, int button = 0)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == button)
            {
                return Mouse.IsOver(rect);
            }

            return false;
        }

        public static bool LeftClicked(Rect rect)
        {
            return Clicked(rect);
        }

        public static bool RightClicked(Rect rect)
        {
            return Clicked(rect, 1);
        }
    }
}
