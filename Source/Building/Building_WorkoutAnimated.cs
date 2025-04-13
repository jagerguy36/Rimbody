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
        public Graphic_Multi graphic = null;
        public int workoutStartTick = -1;
        public float actorMuscle = 25f;
        public WorkOut currentWorkout = null;
        private ModExtensionRimbodyTarget RimbodyEx;

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
            try
            {
                graphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(RimbodyEx.rimbodyBuildingpartGraphic.texPath, ShaderDatabase.DefaultShader, DrawSize, DrawColor);
            }
            catch (Exception) { }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {

            base.DrawAt(drawLoc, flip);
            if(RimbodyEx.rimbodyBuildingpartGraphic != null)
            {
                var vector = DrawPos + RimbodyEx.rimbodyBuildingpartGraphic.drawOffset;
                if (currentWorkout?.useAnimation==true && workoutStartTick > 0)
                {
                    int tickProgress = Find.TickManager.TicksGame - workoutStartTick;
                    if (currentWorkout?.movingpartAnimOffset?.FromRot(base.Rotation) != null && currentWorkout?.movingpartAnimOffset?.FromRot(base.Rotation) != Vector3.zero)
                    {
                        if (tickProgress > 0)
                        {
                            vector += currentWorkout.movingpartAnimOffset.FromRot(base.Rotation);
                        }
                    }

                    if (currentWorkout?.movingpartAnimPeak?.FromRot(base.Rotation) !=null && currentWorkout?.movingpartAnimPeak?.FromRot(base.Rotation) != Vector3.zero)
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
                            vector += JitterVector + nudgeMultiplier * currentWorkout.movingpartAnimPeak.FromRot(base.Rotation);
                        }
                    }
                }
                GetGraphic?.DrawFromDef(vector, this.Rotation, null);
            }
            
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
