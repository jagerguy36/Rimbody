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
            var partgraphic = def.GetModExtension<ModExtensionRimbodyTarget>().rimbodyBuildingpartGraphic;
            if (partgraphic != null)
            {
                GhostUtility.GhostGraphicFor(GraphicDatabase.Get<Graphic_Multi>(partgraphic.texPath, ShaderDatabase.Cutout, def.graphicData.drawSize, Color.white), def, ghostCol).DrawFromDef(GenThing.TrueCenter(loc, rot, def.Size, AltitudeLayer.MetaOverlays.AltitudeFor()), rot, def);
            }
        }
    }
}
