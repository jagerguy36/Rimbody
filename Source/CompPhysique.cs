using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class CompPhysique : ThingComp
    {
        public bool PostGen = false;
        public bool forceRest = false; //For mod compatibility.

        public float BodyFat = -1f;
        public float FatGainFactor = 1f;
        public float FatLoseFactor = 1f;
        public bool useFatgoal = false;
        public float FatGoal = 25f;

        public float MuscleMass = -1f;
        public float MuscleGainFactor = 1f;
        public float MuscleLoseFactor = 1f;
        public bool useMuscleGoal = false;
        public float MuscleGoal = 25f;

        public float gain = 0f;

        public string lastMemory = string.Empty;
        public int lastWorkoutTick = 0;

        public Queue<string> memory = [];
        public CompProperties_Physique Props => (CompProperties_Physique)props;

        private static readonly GeneDef GeneBodyFat = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Fat", true) : null;
        private static readonly GeneDef GeneBodyThin = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Thin", true) : null;
        private static readonly GeneDef GeneBodyHulk = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Hulk", true) : null;
        private static readonly GeneDef GeneBodyStandard = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Standard", true) : null;

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
                if (BodyFat == -1f || MuscleMass == -1f)
                {
                    (BodyFat, MuscleMass) = RandomCompPhysiqueByBodyType(pawn);
                }

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
            Scribe_Values.Look(ref PostGen, "Physique_PostGen", true);
            Scribe_Values.Look(ref forceRest, "Physique_boarded", false);

            Scribe_Values.Look(ref BodyFat, "Physique_BodyFat", -1f);
            Scribe_Values.Look(ref FatGainFactor, "Physique_FatGainFactor", 1f);
            Scribe_Values.Look(ref FatLoseFactor, "Physique_FatLoseFactor", 1f);
            Scribe_Values.Look(ref useFatgoal, "Physique_useFatgoal", false);
            Scribe_Values.Look(ref FatGoal, "Physique_Fatgoal", 25f);

            Scribe_Values.Look(ref MuscleMass, "Physique_MuscleMass", -1f);
            Scribe_Values.Look(ref MuscleGainFactor, "Physique_MuscleGainFactor", 1f);
            Scribe_Values.Look(ref MuscleLoseFactor, "Physique_MuscleLoseFactor", 1f);
            Scribe_Values.Look(ref useMuscleGoal, "Physique_useMuscleGoal", false);
            Scribe_Values.Look(ref MuscleGoal, "Physique_MuscleGoal", 25f);


            Scribe_Values.Look(ref gain, "Physique_gain", 0f);
            Scribe_Values.Look(ref lastMemory, "Physique_lastMemory", string.Empty);
            Scribe_Values.Look(ref lastWorkoutTick, "Physique_lastWorkoutTick", 0);
            Scribe_Collections.Look(ref memory, "Physique_memory", LookMode.Reference);
        }

        public void ResetBody(Pawn pawn)
        {
            if (BodyFat<0 || MuscleMass  < 0) return;

            pawn.story.bodyType = GetValidBody(pawn);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        public virtual BodyTypeDef GetValidBody(Pawn pawn)
        {
            if (ModsConfig.BiotechActive && pawn.DevelopmentalStage.Juvenile())
            {
                if (pawn.DevelopmentalStage.Baby())
                {
                    return BodyTypeDefOf.Baby;
                }

                if (pawn.DevelopmentalStage.Child())
                {
                    return BodyTypeDefOf.Child;
                }
            }
            var fatE = 0.0f;
            if(RimbodySettings.genderDifference && (pawn.gender == Gender.Female))
            {
                fatE = Mathf.Min(RimbodySettings.femaleFatThreshold, 50f-RimbodySettings.fatThresholdFat);
            }

            if (BodyFat > RimbodySettings.fatThresholdFat+ fatE)
            {
                return BodyTypeDefOf.Fat;
            }
            else if (MuscleMass > RimbodySettings.muscleThresholdHulk)
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
                if (pawn.ageTracker?.CurLifeStage?.developmentalStage != DevelopmentalStage.Adult)
                {
                    return true;
                }
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

            var fat = pawn.story.bodyType == BodyTypeDefOf.Fat ? GenMath.RoundTo(Rand.Range(RimbodySettings.fatThresholdFat, 50f), 0.01f) :
                pawn.story.bodyType == BodyTypeDefOf.Hulk ? GenMath.RoundTo(Rand.Range(0f, RimbodySettings.fatThresholdFat-RimbodySettings.gracePeriod), 0.01f) :
                pawn.story.bodyType == BodyTypeDefOf.Thin ? GenMath.RoundTo(Rand.Range(0f, RimbodySettings.fatThresholdThin), 0.01f) :
                GenMath.RoundTo(Rand.Range(RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod, RimbodySettings.fatThresholdFat-RimbodySettings.gracePeriod), 0.01f);

            var muscle = pawn.story.bodyType == BodyTypeDefOf.Hulk ? GenMath.RoundTo(Rand.Range(RimbodySettings.muscleThresholdHulk, 50f), 0.01f) :
                pawn.story.bodyType == BodyTypeDefOf.Thin ? GenMath.RoundTo(Rand.Range(0f, RimbodySettings.muscleThresholdThin), 0.01f) :
                GenMath.RoundTo(Rand.Range(RimbodySettings.muscleThresholdThin+RimbodySettings.gracePeriod, RimbodySettings.muscleThresholdHulk-RimbodySettings.gracePeriod), 0.01f);

            return (Mathf.Clamp(fat, 0f, 40f), Mathf.Clamp(muscle, 0f, 40f));
        }

        public void PhysiqueValueSetup(Pawn pawn)
        {

            if (HARCompat.Active && pawn != null && !HARCompat.CompatibleRace(pawn))
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

        public bool InMemory(string workoutName)
        {
            return memory.Any(item =>
            {
                var parts = item.Split('|');
                return parts.Length > 1 && parts[1] == workoutName;
            });
        }


        public void ApplyGene(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive)
            {
                return;
            }

            if (pawn.genes.HasActiveGene(GeneBodyFat))
            {
                FatGainFactor = 1.25f;
                FatLoseFactor = 0.85f;
            }
            else if (pawn.genes.HasActiveGene(GeneBodyThin))
            {
                FatGainFactor = 0.75f;
                FatLoseFactor = 1.15f;
            }
            else if (pawn.genes.HasActiveGene(GeneBodyHulk))
            {
                MuscleGainFactor = 1.25f;
                MuscleLoseFactor = 0.85f;
            }
            else if (pawn.genes.HasActiveGene(GeneBodyStandard))
            {
                MuscleGainFactor = 1.15f;
                FatGainFactor = 0.85f;
            }
            else
            {
                MuscleGainFactor = 1.0f;
                FatGainFactor = 1.0f;
            }
        }

    }


}
