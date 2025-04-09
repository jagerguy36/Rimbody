using LudeonTK;
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
    internal class Building_WorkoutAnimated: Building
    {
        public Graphic_Multi graphic = null;
        public bool occupied = false;
        public Vector3 nudge = Vector3.zero;

        public Graphic_Multi GetGraphic
        {
            get
            {
                if (graphic is null)
                {
                    LongEventHandler.ExecuteWhenFinished(delegate { GetGraphicLong(); });

                }
                return graphic;
            }
        }

        public void GetGraphicLong()
        {
            try
            {
                graphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(def.building.mechGestatorTopGraphic.texPath, ShaderDatabase.DefaultShader, DrawSize, DrawColor);
            }
            catch (Exception) { }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {

            base.DrawAt(drawLoc, flip);
            var vector = DrawPos + def.building.mechGestatorTopGraphic.drawOffset;
            
            if (occupied)
            {

                vector = vector + nudge;

            }

            GetGraphic?.DrawFromDef(vector, this.Rotation, null);

        }

        public override void Notify_ColorChanged()
        {
            base.Notify_ColorChanged();
            graphic = null;
            Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
            DrawAt(this.DrawPos);
        }
    }
}
