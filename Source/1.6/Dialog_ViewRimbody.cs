using RimWorld;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class Dialog_ViewRimobdy(Pawn editFor) : Window
    {
        public override Vector2 InitialSize => new Vector2(350f, Rimbody.IndividualityLoaded? (Rimbody.WayBetterRomanceLoaded? 340f : 380f) : 290f);

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
            RimbodyCardUtility.DrawRimbodyCard(rect, editFor);
        }
    }
}
