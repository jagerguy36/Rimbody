using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System;
using Verse.Noise;
using static HarmonyLib.Code;

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

            Color barColor = new Color(0.392f, 0.941f, 0.314f, 0.7f);
            Color reserveColor = new Color(1f, 1f, 0.4f, 0.7f);
            Color BackgroundColor = new Color(0f, 0f, 0f, 1f);

            Widgets.ThingIcon(new Rect(0f, 10f, 40f, 40f), pawn);
            Text.Font = GameFont.Medium;
            var rect2 = new Rect(56f, 0f, rect.width, 30f);
            Widgets.Label(rect2, pawn.Name.ToStringShort);
            Text.Font = GameFont.Tiny;
            var num = rect2.y + rect2.height;
            var rect3 = new Rect(56f, num, rect.width, 24f);
            Widgets.Label(rect3, "RimbodyWindow".Translate());
            Text.Font = GameFont.Small;
            num += rect3.height + 10f;

            //Rimbody Info
            var rect8 = new Rect(0f, num, rect.width - 10f, 24f);
            Widgets.Label(new Rect(10f, num + 2f, rect.width - 60f, 24f),
                 "RimbodyMuscleMass".Translate() + ": ");
            Widgets.Label(new Rect(105f, num + 2f, rect.width - 60f, 24f),
                 "" + Math.Round(compPhysique.MuscleMass, 1));
            Rect barBackground1 = new Rect(160f, num + 4f, rect.width - 180f, 16f);
            Widgets.DrawBoxSolid(barBackground1, BackgroundColor);
            Rect filledBar1 = new Rect(barBackground1.x, barBackground1.y, barBackground1.width * (compPhysique.MuscleMass / 50), barBackground1.height);
            Widgets.DrawBoxSolid(filledBar1, barColor);
            TipSignal tip8 = "RimbodyMuscleTooltip".Translate();
            TooltipHandler.TipRegion(rect8, tip8);
            Widgets.DrawHighlightIfMouseover(rect8);


            Rect reservebackground = new Rect(160f, num + 1f, rect.width - 180f, 2f);
            Widgets.DrawBoxSolid(reservebackground, BackgroundColor);
            Rect reservefilledbar = new Rect(reservebackground.x, reservebackground.y, reservebackground.width * (compPhysique.gain / ((2f * compPhysique.MuscleMass * (compPhysique.MuscleGainFactor + (RimbodySettings.genderDifference&&(pawn.gender==Gender.Male)?0.01f:0f))) + 100f)), reservebackground.height);
            Widgets.DrawBoxSolid(reservefilledbar, reserveColor);


            num += rect8.height + 2f;
            var rect9 = new Rect(0f, num, rect.width - 10f, 24f);

            if (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
            {
                Widgets.Label(new Rect(10f, num + 2f, rect.width - 60f, 24f), "└ " + "RimbodyGoal".Translate());
                Rect checkboxRect1 = new Rect(75f, num + 4f, 14f, 14f);
                Widgets.Checkbox(checkboxRect1.x, checkboxRect1.y, ref compPhysique.useMuscleGoal, 14f);

                if (compPhysique.useMuscleGoal)
                {
                    Widgets.Label(new Rect(105f, num + 2f, rect.width - 60f, 24f), "" + compPhysique.MuscleGoal);
                    Rect sliderRect1 = new Rect(153f, num + 4f, rect.width - 165f, 16f);
                    compPhysique.MuscleGoal = (float)Math.Round(Widgets.HorizontalSlider(sliderRect1, compPhysique.MuscleGoal, 0f, 50f), 1);
                    if (ScrolledDown(sliderRect1, true))
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                        compPhysique.MuscleGoal += 0.1f;
                        if (compPhysique.MuscleGoal > 50)
                        {
                            compPhysique.MuscleGoal = 50;
                        }
                    }
                    else if (ScrolledUp(sliderRect1, true))
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                        compPhysique.MuscleGoal -= 0.1f;
                        if (compPhysique.MuscleGoal < 0)
                        {
                            compPhysique.MuscleGoal = 0;
                        }
                    }
                }
            }

            num += rect9.height + 2f;
            var rect10 = new Rect(0f, num, rect.width - 10f, 24f);
            Widgets.Label(new Rect(10f, num + 2f, rect.width - 60f, 24f),
                 "RimbodyBodyFat".Translate() + ": ");
            Widgets.Label(new Rect(105f, num + 2f, rect.width - 60f, 24f),
                 "" + Math.Round(compPhysique.BodyFat, 1));
            Rect barBackground2 = new Rect(160f, num + 4f, rect.width - 180f, 16f);
            Widgets.DrawBoxSolid(barBackground2, BackgroundColor);
            Rect filledBar2 = new Rect(barBackground2.x, barBackground2.y, barBackground2.width * (compPhysique.BodyFat / 50), barBackground2.height);
            Widgets.DrawBoxSolid(filledBar2, barColor);
            TipSignal tip10 = "RimbodyFatTooltip".Translate();
            TooltipHandler.TipRegion(rect10, tip10);
            Widgets.DrawHighlightIfMouseover(rect10);

            num += rect10.height + 2f;
            var rect11 = new Rect(0f, num, rect.width - 10f, 24f);
            if (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
            {
                Widgets.Label(new Rect(10f, num + 2f, rect.width - 60f, 24f), "└ " + "RimbodyGoal".Translate());
                Rect checkboxRect2 = new Rect(75f, num + 4f, 14f, 14f);
                Widgets.Checkbox(checkboxRect2.x, checkboxRect2.y, ref compPhysique.useFatgoal, 14f);

                if (compPhysique.useFatgoal)
                {
                    Widgets.Label(new Rect(105f, num + 2f, rect.width - 60f, 24f), "" + compPhysique.FatGoal);
                    Rect sliderRect2 = new Rect(153f, num + 4f, rect.width - 165f, 16f);
                    compPhysique.FatGoal = (float)Math.Round(Widgets.HorizontalSlider(sliderRect2, compPhysique.FatGoal, 0f, 50f), 1);
                    if (ScrolledDown(sliderRect2, true))
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                        compPhysique.FatGoal += 0.1f;
                        if (compPhysique.FatGoal > 50)
                        {
                            compPhysique.FatGoal = 50;
                        }
                    }
                    else if (ScrolledUp(sliderRect2, true))
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                        compPhysique.FatGoal -= 0.1f;
                        if (compPhysique.FatGoal < 0)
                        {
                            compPhysique.FatGoal = 0;
                        }
                    }
                }
            }

            num += rect11.height + 7f;
            var rect12 = new Rect(10f, num, rect.width, 24f);
            if (editMode)
            {
                GUI.color = Color.red;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect12, "Rimbody_EditModeTooltip".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            num += rect12.height + 5f;

            Texture2D refreshTexture = ContentFinder<Texture2D>.Get("Buttons/RimbodyRefresh");
            if (editMode)
            {
                Rect refreshRect = new Rect(rect.width - 60f, num + 8f, 24f, 24f);
                if (Widgets.ButtonImage(refreshRect, refreshTexture))
                {
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    compPhysique.ResetBody(pawn);
                }
                TipSignal refreshTip = "RimbodyRefreshTooltip".Translate();
                TooltipHandler.TipRegion(refreshRect, refreshTip);
            }

            var rect13 = new Rect((rect.width / 2f) - 90f, num, 180f, 40f);
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
                if (!Widgets.ButtonText(rect13, "Rimbody_EditModeOn".Translate()))
                {
                    return;
                }

                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                editMode = true;

                return;
            }

            if (Widgets.ButtonText(rect13, "Rimbody_EditModeOff".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                editMode = false;
            }

            else if (ScrolledDown(rect8, true) || LeftClicked(rect8))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                compPhysique.MuscleMass += 0.1f;
                if (compPhysique.MuscleMass > 50)
                {
                    compPhysique.MuscleMass = 0;
                }
                compPhysique.ResetBody(pawn);
            }
            else if (ScrolledUp(rect8, true) || RightClicked(rect8))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                compPhysique.MuscleMass -= 0.1f;
                if (compPhysique.MuscleMass < 0)
                {
                    compPhysique.MuscleMass = 50;
                }
                compPhysique.ResetBody(pawn);
            }

            else if (ScrolledDown(rect10, true) || LeftClicked(rect10))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                compPhysique.BodyFat += 0.1f;
                if (compPhysique.BodyFat > 50)
                {
                    compPhysique.BodyFat = 0;
                }
                compPhysique.ResetBody(pawn);
            }
            else if (ScrolledUp(rect10, true) || RightClicked(rect10))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                compPhysique.BodyFat -= 0.1f;
                if (compPhysique.BodyFat < 0)
                {
                    compPhysique.BodyFat = 50;
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
