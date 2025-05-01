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
    public class PlaceWorker_WorkoutAnimated : PlaceWorker
    {

        public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var partgraphics = def.GetModExtension<ModExtensionRimbodyTarget>().rimbodyBuildingpartGraphics;

            if (partgraphics != null && partgraphics.Count > 0)
            {
                foreach (var partgraphic in partgraphics)
                {
                    Graphic_Multi graphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(partgraphic.texPath, ShaderDatabase.Cutout, def.graphicData.drawSize, Color.white);
                    GhostUtility.GhostGraphicFor(graphic, def, ghostCol).DrawFromDef(GenThing.TrueCenter(loc, rot, def.Size, AltitudeLayer.MetaOverlays.AltitudeFor()), def.graphicData.drawRotated? (rot) : Rot4.North, def);//graphicdata.drawRotated? (flip ? Rotation.Opposite : Rotation) : Rot4.North
                }
            }
        }

    }

}
