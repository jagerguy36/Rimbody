using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Verse.PawnRenderNodeProperties;

namespace Maux36.Rimbody
{
    internal class Building_WorkoutAnimated: Building
    {
        public List<Graphic_Multi> graphics = null;
        public int workoutStartTick = -1;
        public float actorMuscle = 25f;
        public WorkOut currentWorkout = null;
        private ModExtensionRimbodyTarget RimbodyEx;

        public List<Graphic_Multi> GetGraphic
        {
            get
            {
                if (graphics is null)
                {
                    LongEventHandler.ExecuteWhenFinished(delegate { GetGraphicLong(); });
                }
                return graphics;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workoutStartTick, "workoutStartTick");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            RimbodyEx = this.def.GetModExtension<ModExtensionRimbodyTarget>();
        }

        public void GetGraphicLong()
        {
            graphics = [];
            try
            {
                Log.Message("Get graphic!");
                // Loop through each texPath in the list of RimbodyEx.rimbodyBuildingpartGraphic
                foreach (var buildingPartGraphic in RimbodyEx.rimbodyBuildingpartGraphics)
                {
                    // Retrieve the graphic for each texPath and add to the graphic list
                    var newGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(buildingPartGraphic.texPath, ShaderDatabase.DefaultShader, DrawSize, DrawColor);
                    graphics.Add(newGraphic);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to get graphics: " + ex.Message);
            }
        }



        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {

            base.DrawAt(drawLoc, flip);
            Vector3 calculatedOffset = Vector3.zero;
            if (RimbodyEx.rimbodyBuildingpartGraphics != null)
            {
                if (currentWorkout?.useAnimation == true && workoutStartTick > 0)
                {
                    int tickProgress = Find.TickManager.TicksGame - workoutStartTick;
                    if (currentWorkout?.movingpartAnimOffset?.FromRot(base.Rotation) != null && currentWorkout?.movingpartAnimOffset?.FromRot(base.Rotation) != Vector3.zero)
                    {
                        if (tickProgress > 0)
                        {
                            calculatedOffset += currentWorkout.movingpartAnimOffset.FromRot(base.Rotation);
                        }
                    }

                    if (currentWorkout?.movingpartAnimPeak?.FromRot(base.Rotation) != null && currentWorkout?.movingpartAnimPeak?.FromRot(base.Rotation) != Vector3.zero)
                    {
                        float uptime = 0.95f - (15f * actorMuscle / 5000f);
                        float cycleDuration = 125f - actorMuscle;
                        float jitter_amount = 3f * Mathf.Max(0f, (1f - (actorMuscle / 35f))) / 100f;
                        float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                        float nudgeMultiplier;
                        if (cycleTime < uptime)
                        {
                            nudgeMultiplier = Mathf.Lerp(0f, 1f, cycleTime / uptime);
                        }
                        else
                        {
                            nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - uptime) / (1f - uptime));
                        }

                        float xJitter = (Rand.RangeSeeded(-jitter_amount, jitter_amount, tickProgress));
                        Vector3 JitterVector = IntVec3.West.RotatedBy(base.Rotation).ToVector3() * xJitter;
                        if (tickProgress > 0)
                        {
                            calculatedOffset += JitterVector + nudgeMultiplier * currentWorkout.movingpartAnimPeak.FromRot(base.Rotation);
                        }
                    }
                }
                if (GetGraphic != null && GetGraphic.Count>0)
                {
                    for (int i = 0; i < GetGraphic.Count && i < RimbodyEx.rimbodyBuildingpartGraphics.Count; i++)
                    {
                        var part_graphic = GetGraphic[i];  // Get the Graphic_Multi at index i
                        var graphicdata = RimbodyEx.rimbodyBuildingpartGraphics[i];  // Get the corresponding GraphicData at index i

                        Log.Message($"Getgraphic.count = {GetGraphic.Count}, {RimbodyEx.rimbodyBuildingpartGraphics.Count}. {part_graphic.GraphicPath}");
                        // Assuming DrawFromDef expects a position and rotation to draw the graphic
                        part_graphic.DrawFromDef(calculatedOffset, this.Rotation, null);//graphicdata.drawOffset + 
                    }
                }
            }
        }

        public override void Notify_ColorChanged()
        {
            base.Notify_ColorChanged();
            graphics = null;
            Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
            DrawAt(this.DrawPos);
        }
    }
}
