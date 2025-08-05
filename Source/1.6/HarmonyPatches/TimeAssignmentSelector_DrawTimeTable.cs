using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Maux36.Rimbody
{
    public class TimeAssignmentSelector_DrawTimeTable_Patch
    {
        private static TimeAssignmentDef selectedTADef = TimeAssignmentDefOf.Joy;
        [HarmonyPatch(typeof(TimeAssignmentSelector), nameof(TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid))]
        public static class TimeAssignmentSelector_DrawTimeTable_Postfix
        {
            private static TimeAssignmentDef selectedTADef = TimeAssignmentDefOf.Joy;
            public static void Postfix(Rect rect)
            {
                if (RimbodySettings.useRecToSelect)
                {
                    return;
                }
                Rect rect2 = rect;
                rect2.xMax = rect2.center.x;
                rect2.yMax = rect2.center.y;
                rect2.x += (4 + 0) * rect2.width;
                if (ModsConfig.RoyaltyActive)
                    rect2.x += rect2.width;
                if (Rimbody.ExosuitFrameworkLoaded)
                    rect2.x += rect2.width;
                DrawTimeAssignmentSelectorFor(rect2, DefOf_Rimbody.Rimbody_Workout);
            }

            public static void DrawTimeAssignmentSelectorFor(Rect rect, TimeAssignmentDef ta)
            {
                rect = rect.ContractedBy(2f);
                GUI.DrawTexture(rect, ta.ColorTexture);
                if (Widgets.ButtonInvisible(rect, true))
                {
                    TimeAssignmentSelector.selectedAssignment = ta;
                    selectedTADef = ta;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                GUI.color = Color.white;
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.white;
                Widgets.Label(rect, ta.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                if (TimeAssignmentSelector.selectedAssignment == ta)
                {
                    Widgets.DrawBox(rect, 2);
                    return;
                }
                UIHighlighter.HighlightOpportunity(rect, ta.cachedHighlightNotSelectedTag);
            }
        }

        [HarmonyPatch(typeof(TimeAssignmentSelector), nameof(TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid))]
        public static class TimeAssignmentSelector_DrawTimeAssignmentSelectorGrid_Transpiler
        {
            static MethodInfo targetMethod = AccessTools.Method(typeof(TimeAssignmentSelector), "DrawTimeAssignmentSelectorFor");
            static FieldInfo joyField = AccessTools.Field(typeof(TimeAssignmentDefOf), nameof(TimeAssignmentDefOf.Joy));
            static MethodInfo RecOrWorKoutTabDrawMethod = AccessTools.Method(typeof(TimeAssignmentSelector_DrawTimeTable_Patch), nameof(TimeAssignmentSelector_DrawTimeTable_Patch.DrawTimeAssignmentSelectorOption));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldsfld
                        && code[i].operand is FieldInfo fi
                        && fi == joyField
                        && i + 1 < code.Count
                        && code[i + 1].Calls(targetMethod)
                        )
                    {
                        code[i + 1] = new CodeInstruction(OpCodes.Call, RecOrWorKoutTabDrawMethod);
                        break;
                    }
                }
                return code;
            }
        }

        public static void DrawTimeAssignmentSelectorOption(Rect rect, TimeAssignmentDef ta)
        {
            rect = rect.ContractedBy(2f);

            if (RimbodySettings.useRecToSelect)
            {
                GUI.DrawTexture(rect, selectedTADef.ColorTexture);
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                TextAnchor oldAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, selectedTADef.LabelCap);
                Text.Anchor = oldAnchor;
                if (TimeAssignmentSelector.selectedAssignment == selectedTADef)
                {
                    Widgets.DrawBox(rect, 2);
                }
                if (!Widgets.ButtonInvisible(rect))
                {
                    return;
                }
                List<FloatMenuOption> list = new List<FloatMenuOption>
                {
                    new FloatMenuOption(DefOf_Rimbody.Rimbody_Workout.LabelCap, () => SetTimeAssignment(DefOf_Rimbody.Rimbody_Workout)),
                    new FloatMenuOption(TimeAssignmentDefOf.Joy.LabelCap, () => SetTimeAssignment(TimeAssignmentDefOf.Joy))
                };

                Find.WindowStack.Add(new FloatMenu(list));
            }
            else
            {
                GUI.DrawTexture(rect, ta.ColorTexture);
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                if (Widgets.ButtonInvisible(rect))
                {
                    TimeAssignmentSelector.selectedAssignment = ta;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
                using (new TextBlock(TextAnchor.MiddleCenter))
                {
                    Widgets.Label(rect, ta.LabelCap);
                }
                if (TimeAssignmentSelector.selectedAssignment == ta)
                {
                    Widgets.DrawBox(rect, 2);
                }
                else
                {
                    UIHighlighter.HighlightOpportunity(rect, ta.cachedHighlightNotSelectedTag);
                }

            }
        }
        private static void SetTimeAssignment(TimeAssignmentDef def)
        {
            selectedTADef = def;
            TimeAssignmentSelector.selectedAssignment = def;
        }
    }
}

