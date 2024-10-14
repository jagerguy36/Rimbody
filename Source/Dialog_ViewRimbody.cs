using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class Dialog_ViewRimobdy(Pawn editFor) : Window
    {
        public override Vector2 InitialSize => new Vector2(350f, Rimbody.IndividualityLoaded? 330f : 250f);

        public override void DoWindowContents(Rect inRect)
        {
            soundClose = SoundDefOf.InfoCard_Close;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            forcePause = true;
            preventCameraMotion = false;
            doCloseX = true;
            closeOnAccept = true;
            closeOnCancel = true;
            var rect = new Rect(inRect.x - 10f, inRect.y, inRect.width + 10f, inRect.height);
            if (Find.WindowStack.IsOpen(typeof(Dialog_Trade)) || Current.ProgramState != ProgramState.Playing)
            {
                RimbodyCardUtility.DrawRimbodyCard(rect, editFor);
            }
            else
            {
                RimbodyCardUtility.DrawRimbodyCard(rect, Find.Selector.SingleSelectedThing as Pawn);
            }
        }
    }
}
