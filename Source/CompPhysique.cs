using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class CompPhysique : ThingComp
    {
        public float BodyFat = -1f;
        public float MuscleMass = -1f;
        public float FatGainFactor = 1f;
        public float FatLoseFactor = 1f;
        public float MuscleGainFactor = 1f;
        public float MuscleLoseFactor = 1f;
        public float gain = 0f;
        public int lastWorkoutTick = 0;
        public string lastMemory = string.Empty;
        public Queue<string> memory = [];

        public CompProperties_Physique Props => (CompProperties_Physique)props;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            var pawn = parent as Pawn;
            PhysiqueValueSetup(pawn);
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (signal == "bodyTypeSelected" && parent is Pawn pawn)
            {
                Log.Message($"Body Type Selected for {pawn.Name}");
                if (BodyFat == -1f || MuscleMass == -1f)
                {
                    (BodyFat, MuscleMass) = RandomCompPhysiqueByBodyType(pawn);
                }
                //else if (!IsValidBodyType(pawn)){
                //    Log.Message("Invalid BodyType. Resetting.");
                //    ResetBody(pawn);
                //}

            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var pawn = parent as Pawn;
            PhysiqueValueSetup(pawn);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref BodyFat, "Physique_BodyFat", -1f);
            Scribe_Values.Look(ref MuscleMass, "Physique_MuscleMass", -1f);
            Scribe_Values.Look(ref gain, "Physique_gain", 0f);
            Scribe_Values.Look(ref FatGainFactor, "Physique_FatGainFactor", 1f);
            Scribe_Values.Look(ref FatLoseFactor, "Physique_FatLoseFactor", 1f);
            Scribe_Values.Look(ref MuscleGainFactor, "Physique_MuscleGainFactor", 1f);
            Scribe_Values.Look(ref MuscleLoseFactor, "Physique_MuscleLoseFactor", 1f);
            Scribe_Values.Look(ref lastWorkoutTick, "Physique_lastWorkoutTick", 0);
            Scribe_Values.Look(ref lastMemory, "Physique_lastMemory", string.Empty);
            Scribe_Collections.Look(ref memory, "Physique_memory", LookMode.Value);
        }

        public void ResetBody(Pawn pawn)
        {
            pawn.story.bodyType = GetValidBody(pawn);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        public virtual BodyTypeDef GetValidBody(Pawn pawn)
        {
            if (BodyFat > RimbodySettings.fatThresholdFat)
            {
                return BodyTypeDefOf.Fat;
            }
            else if (MuscleMass > RimbodySettings.muscleThresholdHulk && RimbodySettings.fatThresholdFat >= BodyFat)
            {
                return BodyTypeDefOf.Hulk;
            }
            else if (0 <= BodyFat && BodyFat < RimbodySettings.fatThresholdThin && 0 <= MuscleMass && MuscleMass < RimbodySettings.muscleThresholdThin)
            {
                return BodyTypeDefOf.Thin;
            }
            else if (0 <= BodyFat && BodyFat <= 50 && 0 <= MuscleMass && MuscleMass <= 50)
            {
                return pawn.gender == Gender.Male ? BodyTypeDefOf.Male : BodyTypeDefOf.Female;
            }
            else
            {
                return pawn.story.bodyType;
            }
        }

        public bool IsValidBodyType(Pawn pawn)
        {
            if (pawn?.story?.bodyType != null)
            {
                if (pawn.story.bodyType == BodyTypeDefOf.Fat)
                {
                    if (BodyFat > RimbodySettings.fatThresholdFat)
                    {
                        return true;
                    }
                    return false;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Hulk)
                {
                    if (MuscleMass > RimbodySettings.muscleThresholdHulk && RimbodySettings.fatThresholdFat >= BodyFat)
                    {
                        return true;
                    }
                    return false;
                }
                else if (pawn.story.bodyType == BodyTypeDefOf.Thin)
                {
                    if (0 <= BodyFat && BodyFat < RimbodySettings.fatThresholdThin && 0 <= MuscleMass && MuscleMass < RimbodySettings.muscleThresholdThin)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (BodyFat <= 40 && MuscleMass <= 40)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }

        public (float, float) RandomCompPhysiqueByBodyType(Pawn pawn)
        {
            if (pawn?.story?.bodyType == null)
            {
                return (-1f, -1f);
            }

            var fat = pawn.story.bodyType == BodyTypeDefOf.Fat ? GenMath.RoundTo(Rand.Range(30f, 40f), 0.1f) :
                pawn.story.bodyType == BodyTypeDefOf.Hulk ? GenMath.RoundTo(Rand.Range(10f, 20f), 0.1f) :
                pawn.story.bodyType == BodyTypeDefOf.Thin ? GenMath.RoundTo(Rand.Range(0f, 10f), 0.1f) :
                GenMath.RoundTo(Rand.Range(15f, 30f), 0.1f);

            var muscle = pawn.story.bodyType == BodyTypeDefOf.Hulk ? GenMath.RoundTo(Rand.Range(30f, 40f), 0.1f) :
                pawn.story.bodyType == BodyTypeDefOf.Thin ? GenMath.RoundTo(Rand.Range(0f, 10f), 0.1f) :
                GenMath.RoundTo(Rand.Range(15f, 30f), 0.1f);

            return (Mathf.Clamp(fat, 0f, 40f), Mathf.Clamp(muscle, 0f, 40f));
        }

        public void PhysiqueValueSetup(Pawn pawn)
        {
            if (HARCompat.Active && pawn != null && pawn.def.defName!="Human")
            {
                BodyFat = -2f;
                MuscleMass = -2f;
            }

            if (pawn != null && (BodyFat == -1f || MuscleMass == -1f))
            {
                (BodyFat, MuscleMass) = RandomCompPhysiqueByBodyType(pawn);
            }
        }

        public void AddNewMemory(String workout)
        {
            this.lastMemory = workout;
            if (this.memory.Count >= 3)
            {
                memory.Dequeue();
            }
            memory.Enqueue(workout);
        }
    }


}
