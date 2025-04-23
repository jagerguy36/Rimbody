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
        public int currentWorkoutIndex = -1;
        public float actorMuscle = 25f;
        public bool useJitter= true;

        private static readonly string[] muscleGroups = {
            "Rimbody_Shoulder",
            "Rimbody_Chest",
            "Rimbody_Biceps",
            "Rimbody_Triceps",
            "Rimbody_Back",
            "Rimbody_Core",
            "Rimbody_Glutes",
            "Rimbody_Quads",
            "Rimbody_Hams"
        };

        public WorkOut CurrentWorkout
        {
            get
            {
                // Return the workout if the index is valid, otherwise return null
                return (currentWorkoutIndex >= 0 && currentWorkoutIndex < RimbodyEx.workouts.Count)
                    ? RimbodyEx.workouts[currentWorkoutIndex]
                    : null;
            }
        }
        private ModExtensionRimbodyTarget RimbodyEx;

        public override string DescriptionFlavor
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(base.DescriptionFlavor);
                if(RimbodyEx != null)
                {
                    stringBuilder.Append("\n\n" + "Rimbody_Description".Translate() + "\n");
                    foreach (WorkOut wo in RimbodyEx.workouts)
                    {
                        stringBuilder.Append($"\n{wo.name.Translate()}:");
                        //for (int i = 0; i < wo.strengthParts.Count; i++)
                        //{
                        //    if (i >= 1)
                        //    {
                        //        stringBuilder.Append($"{muscleGroups[i]}({wo.strengthParts[i]})");
                        //    }
                        //}
                        var topMuscles = wo.strengthParts
                            .Select((value, index) => new { Index = index, Value = value })
                            .Where(x => x.Value > 0)
                            .OrderByDescending(x => x.Value)
                            .Take(3);

                        foreach (var item in topMuscles)
                        {
                            stringBuilder.Append($" {muscleGroups[item.Index].Translate()}({item.Value})");
                        }

                    }


                }

                return stringBuilder.ToString();
            }
        }

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
            Scribe_Values.Look(ref workoutStartTick, "workoutStartTick", -1);
            Scribe_Values.Look(ref currentWorkoutIndex, "currentWorkoutIndex", -1);
            Scribe_Values.Look(ref actorMuscle, "actorMuscle", 25f);
            Scribe_Values.Look(ref useJitter, "useJitter", true);
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
                foreach (var buildingPartGraphic in RimbodyEx.rimbodyBuildingpartGraphics)
                {
                    var newGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(buildingPartGraphic.texPath, buildingPartGraphic.shaderType!=null?buildingPartGraphic.shaderType.Shader:ShaderDatabase.DefaultShader, DrawSize, DrawColor);
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
            Vector3 calculatedOffset = Vector3.zero;
            if (RimbodyEx.rimbodyBuildingpartGraphics != null)
            {
                // During Workout
                if (CurrentWorkout?.animationType == InteractionType.animation && workoutStartTick > 0)
                {
                    int tickProgress = Find.TickManager.TicksGame - workoutStartTick;
                    if (CurrentWorkout?.movingpartAnimOffset?.FromRot(base.Rotation) != null && CurrentWorkout?.movingpartAnimOffset?.FromRot(base.Rotation) != Vector3.zero)
                    {
                        if (tickProgress > 0)
                        {
                            calculatedOffset += CurrentWorkout.movingpartAnimOffset.FromRot(base.Rotation);
                        }
                    }

                    if (CurrentWorkout?.movingpartAnimPeak?.FromRot(base.Rotation) != null && CurrentWorkout?.movingpartAnimPeak?.FromRot(base.Rotation) != Vector3.zero)
                    {
                        float uptime = 0.95f - (20f * actorMuscle / 5000f);
                        float cycleDuration = 125f - actorMuscle;
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
                        Vector3 JitterVector = Vector3.zero;
                        if (useJitter)
                        {
                            JitterVector = IntVec3.West.RotatedBy(base.Rotation).ToVector3();
                            float jitter_amount = 3f * Mathf.Max(0f, (1f - (actorMuscle / 35f))) / 100f;
                            float xJitter = (Rand.RangeSeeded(-jitter_amount, jitter_amount, tickProgress));
                            JitterVector = JitterVector * xJitter;
                        }
                        if (tickProgress > 0)
                        {
                            calculatedOffset += JitterVector + nudgeMultiplier * CurrentWorkout.movingpartAnimPeak.FromRot(base.Rotation);
                        }
                    }
                }
                if (GetGraphic != null && GetGraphic.Count>0)
                {
                    for (int i = 0; i < GetGraphic.Count && i < RimbodyEx.rimbodyBuildingpartGraphics.Count; i++)
                    {
                        var part_graphic = GetGraphic[i];  // Get the Graphic_Multi at index i
                        var graphicdata = RimbodyEx.rimbodyBuildingpartGraphics[i];  // Get the corresponding GraphicData at index i
                        //Assuming DrawFromDef expects a position and rotation to draw the graphic
                        part_graphic.Draw(DrawPos + graphicdata.drawOffset + calculatedOffset, flip ? Rotation.Opposite : Rotation, this);
                    }
                }
            }
            if (RimbodyEx.moveBase)
            {
                base.DrawAt(drawLoc + calculatedOffset, flip);
            }
            else
            {
                base.DrawAt(drawLoc, flip);
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
