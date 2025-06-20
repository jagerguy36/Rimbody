using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class Building_WorkoutAnimated: Building
    {
        public bool beingUsed = false;
        private string cachedDescriptionFlavor = null;
        public List<Graphic_Multi> graphics = null;
        public Vector3 calculatedOffset = Vector3.zero;
        public Vector3 DrawAtOffset => beingUsed ? calculatedOffset : Vector3.zero;

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
        private ModExtensionRimbodyTarget RimbodyExInternal;
        private ModExtensionRimbodyTarget RimbodyEx
        {
            get
            {
                RimbodyExInternal ??= def.GetModExtension<ModExtensionRimbodyTarget>();
                return RimbodyExInternal;
            }
        }
        public override string DescriptionFlavor
        {
            get
            {
                if (cachedDescriptionFlavor != null)
                {
                    return cachedDescriptionFlavor;
                }
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(base.DescriptionFlavor);
                if (RimbodyEx != null)
                {
                    stringBuilder.Append("\n\n" + "Rimbody_Description".Translate() + "\n");
                    foreach (WorkOut wo in RimbodyEx.workouts)
                    {
                        stringBuilder.Append($"\n{wo.name.Translate()}");
                        if (wo.Category == RimbodyWorkoutCategory.Strength || wo.Category == RimbodyWorkoutCategory.Balance)
                        {
                            stringBuilder.Append($": ");
                            var topMuscles = wo.strengthParts
                                .Select((value, index) => new { index, value })
                                .Where(x => x.value > 0)
                                .OrderByDescending(x => x.value)
                                .Take(3)
                                .Select(item => muscleGroups[item.index].Translate())
                                .Aggregate((a, b) => a + ", " + b);
                            stringBuilder.Append(topMuscles);
                        }
                    }
                }
                cachedDescriptionFlavor = stringBuilder.ToString();
                return cachedDescriptionFlavor;
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
            Scribe_Values.Look(ref beingUsed, "bwa_beingUsed", false);
            Scribe_Values.Look(ref calculatedOffset, "bwa_calculatedOffset", Vector3.zero);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
        public void GetGraphicLong()
        {
            graphics = [];
            if (RimbodyEx.rimbodyTargetpartGraphics != null)
            {
                try
                {
                    foreach (var targetPartGraphic in RimbodyEx.rimbodyTargetpartGraphics)
                    {
                        var newGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(targetPartGraphic.texPath, targetPartGraphic.shaderType != null ? targetPartGraphic.shaderType.Shader : ShaderDatabase.DefaultShader, DrawSize, DrawColor);
                        graphics.Add(newGraphic);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to get graphics: " + ex.Message);
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            for (int i = 0; i < GetGraphic.Count; i++)
            {
                var graphicdata = RimbodyEx.rimbodyTargetpartGraphics[i];
                GetGraphic[i].Draw(DrawPos + graphicdata.drawOffset + DrawAtOffset, graphicdata.drawRotated ? (flip ? Rotation.Opposite : Rotation) : Rot4.North, this);
            }
            if (RimbodyEx.moveBase)
            {
                base.DrawAt(drawLoc + DrawAtOffset, flip);
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
