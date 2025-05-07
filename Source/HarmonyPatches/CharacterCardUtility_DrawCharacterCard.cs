using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Maux36.Rimbody
{
    //Code refrenced from individuality by Syrchalis
    [HarmonyPatch(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard))]
    public static class CharacterCardUtility_DrawCharacterCard
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var operand = AccessTools.Method(typeof(CharacterCardUtility_DrawCharacterCard),
                nameof(RimbodyCardButton));
            var list = instructions.ToList();
            var num = list.FindIndex(ins =>
                ins.IsStloc() && ins.operand is LocalBuilder { LocalIndex: 20 });
            list.InsertRange(num + 1, new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloca, 20),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Call, operand)
        });
            return list;
        }

        public static void RimbodyCardButton(ref float x, Rect rect, Pawn pawn, Rect creationRect)
        {
            if (pawn == null)
            {
                return;
            }

            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (compPhysique == null || compPhysique.MuscleMass <= -1 || compPhysique.BodyFat <= -1)
            {
                return;
            }

            //IF Individuality loaded then return the modified one.

            TipSignal tip = "RimbodyTooltip".Translate();
            var rect2 = new Rect(x, 2.7f, 24f, 24f);
            x -= 40f;
            if (Current.ProgramState != ProgramState.Playing)
            {
                rect2 = new Rect(creationRect.width - 24f, 80f, 24f, 24f);
            }

            var color = GUI.color;
            GUI.color = rect2.Contains(Event.current.mousePosition)
                ? new Color(0.25f, 0.59f, 0.75f)
                : new Color(1f, 1f, 1f);

            GUI.DrawTexture(rect2, ContentFinder<Texture2D>.Get("Buttons/Rimbody"));
            TooltipHandler.TipRegion(rect2, tip);
            if (Widgets.ButtonInvisible(rect2))
            {
                SoundDefOf.InfoCard_Open.PlayOneShotOnCamera();
                Find.WindowStack.Add(new Dialog_ViewRimobdy(pawn));
            }

            GUI.color = color;
        }
    }
}
