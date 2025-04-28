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
    public class PlaceWorker_ShowDirection : PlaceWorker
    {

        private static readonly string IconPath = "UI/PlaceWorker/FacingDir";

        private static readonly Material IconMat;
        private static readonly MaterialPropertyBlock GhostBlock;

        static PlaceWorker_ShowDirection()
        {
            IconMat = MaterialPool.MatFrom(IconPath, ShaderDatabase.MetaOverlay);
            GhostBlock = new MaterialPropertyBlock();
            GhostBlock.SetColor("_Color", new Color(1f, 1f, 1f, 0.35f));
        }

        public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Vector3 offset = rot.FacingCell.ToVector3() * 0.1f;
            Vector3 drawPos = loc.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays) + offset;
            Quaternion rotation = Quaternion.Euler(0f, rot.Opposite.AsAngle, 0f);

            Graphics.DrawMesh(MeshPool.plane10, drawPos, rotation, IconMat, 0, null, 0, GhostBlock);
        }
    }

}
