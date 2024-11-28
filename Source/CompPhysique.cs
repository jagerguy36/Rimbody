using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class CompPhysique : ThingComp
    {
        private Pawn parentPawnInt = null;
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
        public float carryFactor = 0f;

        public Queue<string> memory = [];
        public CompProperties_Physique Props => (CompProperties_Physique)props;

        private static readonly GeneDef GeneBodyFat = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Fat", true) : null;
        private static readonly GeneDef GeneBodyThin = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Thin", true) : null;
        private static readonly GeneDef GeneBodyHulk = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Hulk", true) : null;
        private static readonly GeneDef GeneBodyStandard = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Standard", true) : null;

        private static readonly GeneDef NonSenescent = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("DiseaseFree", true) : null;
        private static readonly HashSet<string> RimbodyJobs =
        [
            "Rimbody_DoStrengthBuilding",
            "Rimbody_DoBalanceBuilding",
            "Rimbody_DoCardioBuilding",
        ];

        private Pawn parentPawn
        {
            get
            {
                parentPawnInt ??= parent as Pawn;
                return parentPawnInt;
            }
        }

        private void PhysiqueTick(float forcedCardio = -1f, float forcedStrength = -1f)
        {
            //Cardio
            //private static readonly float _sprintC = 2.0f; 1.8 when limited
            //private static readonly float _hardworkC = 1.0f; construction, smoothing, mining, replanting, extractTrees
            //private static readonly float _workC = 0.85f; harvesting, repairing, empty wasters, clean pollutions
            //private static readonly float _joggingC = 0.8f;
            //private static readonly float _walkingC = 0.4f; light physical works
            //private static readonly float _ambleC = 0.35f;
            //private static readonly float _baseC = 0.3f; sedentary works, standing
            //private static readonly float _sleepC = 0.2f; laying down

            //Strength
            //private static readonly float _workoutS = 2.0f;  1.8 when limited
            //private static readonly float _hardworkS = 1.2f;
            //private static readonly float _workS = 0.8f;
            //private static readonly float _sprintS = 0.25f;
            //private static readonly float _movingS = 0.2f; light physical works
            //private static readonly float _ambleS = 0.15f;
            //private static readonly float _sedentaryS = 0.1f; sedentary, standing
            //private static readonly float _lyingS = 0.0f; laying down

            var curJobDef = parentPawn.CurJobDef;

            if (parentPawn?.needs?.food != null && (parentPawn.IsColonistPlayerControlled || parentPawn.IsPrisonerOfColony || parentPawn.IsSlaveOfColony || parentPawn.IsColonist && parentPawn.GetCaravan() != null || (parentPawn.IsColonist && curJobDef != null)))
            {
                if (BodyFat <= -1f || MuscleMass <= -1f)
                {
                    return;
                }

                var tickRatio = RimbodySettings.CalcEveryTick / 150f;
                var frozenProperty = typeof(Need).GetProperty("IsFrozen", BindingFlags.NonPublic | BindingFlags.Instance);
                var isFoodNeedFrozen = (bool)frozenProperty.GetValue(parentPawn.needs.food);
                if (isFoodNeedFrozen)
                {
                    return;
                }

                var curFood = Mathf.Clamp(parentPawn.needs.food.CurLevelPercentage, 0f, 1f);
                var curDriver = parentPawn.jobs?.curDriver;
                var checkFlag = false;

                var pawnCaravan = parentPawn.GetCaravan();

                float newBodyFat;
                float newMuscleMass;


                float cardioFactor = 0.3f; //_baseC
                float strengthFactor = 0.1f; //_sedentaryS

                //If factor is Forced
                if (forcedCardio >= 0 && forcedStrength >=0)
                {
                    cardioFactor = forcedCardio;
                    strengthFactor = forcedStrength;
                }
                //Factors based on jobs
                else
                {
                    //Factors for Caravan
                    if (pawnCaravan != null)
                    {
                        //Resting
                        if (!pawnCaravan.pather.MovingNow || parentPawn.InCaravanBed() || parentPawn.CarriedByCaravan())
                        {
                            cardioFactor = 0.2f;
                            strengthFactor = 0.0f;
                        }
                        //Moving, but not boarded
                        else if (pawnCaravan.pather.MovingNow)
                        {
                            cardioFactor = 0.4f;  //Walking
                            strengthFactor = 0.2f;
                        }
                    }
                    else if (curJobDef != null && curDriver != null)
                    {
                        //get work factor
                        var jobExtension = curJobDef.GetModExtension<ModExtentionRimbodyJob>();
                        var giverExtension = parentPawn?.CurJob?.workGiverDef?.GetModExtension<ModExtentionRimbodyJob>();
                        //Special cases: moving
                        if (parentPawn.pather?.MovingNow == true)
                        {
                            switch (parentPawn.jobs.curJob.locomotionUrgency)
                            {
                                case LocomotionUrgency.Sprint:
                                    {
                                        cardioFactor = 2.0f; //_sprintC;
                                        strengthFactor = 0.25f + carryFactor;
                                    }
                                    break;
                                case LocomotionUrgency.Jog:
                                    {
                                        cardioFactor = 0.8f; //_joggingC;
                                        strengthFactor = 0.2f + carryFactor;
                                    }
                                    break;
                                case LocomotionUrgency.Walk:
                                    {
                                        cardioFactor = 0.4f; //_walkingC;
                                        strengthFactor = 0.2f + carryFactor;
                                    }
                                    break;
                                default:
                                    {
                                        cardioFactor = 0.35f; //_ambleC
                                        strengthFactor = 0.15f + carryFactor;
                                    }
                                    break;
                            }
                        }
                        //Special cases: Lying down
                        else if (curDriver?.CurToilString == "LayDown")
                        {
                            cardioFactor = 0.2f;
                            strengthFactor = 0.0f;
                        }
                        //Get factors from dedicated Rimbody buildings
                        else if (RimbodyJobs.Contains(curJobDef.defName))
                        {
                            var curjobTargetDef = parentPawn.CurJob.targetA.Thing.def;
                            var curjobTargetName = curjobTargetDef.defName;
                            var buildingExtention = curjobTargetDef.GetModExtension<ModExtentionRimbodyBuilding>();
                            if (buildingExtention != null)
                            {
                                cardioFactor = buildingExtention.cardio;
                                strengthFactor = buildingExtention.strength;
                                if (InMemory(curjobTargetName))
                                {
                                    cardioFactor = cardioFactor * 0.9f;
                                    strengthFactor = strengthFactor * 0.9f;
                                }
                            }

                        }
                        else if (jobExtension != null)
                        {
                            cardioFactor = jobExtension.cardio;
                            strengthFactor = jobExtension.strength;
                        }
                        else if (giverExtension != null)
                        {
                            cardioFactor = giverExtension.cardio;
                            strengthFactor = giverExtension.strength;
                        }
                        //Log.Message($"{parentPawn.Name} doing {curJobDef.defName}. Factors: s:{strengthFactor}, c:{cardioFactor}");
                    }
                }


                if (forceRest)
                {
                    cardioFactor = 0.2f;
                    strengthFactor = 0.0f;
                }

                //Tiredness reduces gain
                if (parentPawn.needs.rest != null)
                {
                    switch (parentPawn.needs.rest.CurCategory)
                    {
                        case RestCategory.Tired:
                            strengthFactor -= 0.8f;
                            break;
                        case RestCategory.VeryTired:
                            strengthFactor -= 1.2f;
                            break;
                        case RestCategory.Exhausted:
                            strengthFactor -= 2f;
                            break;
                        default:
                            break;
                    }
                }

                //Gain and Lose Factor
                var fatgainF = FatGainFactor;
                var fatloseF = FatLoseFactor;
                var musclegainF = MuscleGainFactor;
                var muscleloseF = MuscleLoseFactor;
                float fatThreshholdE = 0.0f;

                //Gender
                if (RimbodySettings.genderDifference == true)
                {
                    if (parentPawn.gender == Gender.Male)
                    {
                        musclegainF += RimbodySettings.maleMusclegain;
                    }
                    else
                    {
                        fatThreshholdE = RimbodySettings.femaleFatThreshold;
                    }
                }
                //Age Factors in
                switch (parentPawn.ageTracker?.CurLifeStage?.developmentalStage)
                {
                    case DevelopmentalStage.None:
                        break;

                    case DevelopmentalStage.Newborn:
                        fatgainF -= 0.024f;
                        fatloseF += 0.024f;

                        musclegainF += 0.125f;
                        muscleloseF -= 0.125f;
                        break;

                    case DevelopmentalStage.Baby:
                        fatgainF -= 0.024f;
                        fatloseF += 0.024f;

                        musclegainF += 0.125f;
                        muscleloseF -= 0.125f;
                        break;

                    case DevelopmentalStage.Child:
                        fatgainF -= 0.024f;
                        fatloseF += 0.024f;

                        musclegainF += 0.06f;
                        muscleloseF -= 0.06f;
                        break;

                    case DevelopmentalStage.Adult:
                        if (NonSenescent != null && parentPawn.genes.HasActiveGene(NonSenescent))
                        {
                            fatgainF -= (float)(Math.Min(parentPawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint) - 25) / 500f;
                            fatloseF += (float)(Math.Min(parentPawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint) - 25) / 500f;

                            musclegainF += (float)(Math.Min(parentPawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint) - 25) / 200f;
                            muscleloseF -= (float)(Math.Min(parentPawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint) - 25) / 200f;
                        }
                        else
                        {
                            fatgainF -= (float)(Math.Min(parentPawn.ageTracker.AgeBiologicalYears, 125) - 25) / 500f;
                            fatloseF += (float)(Math.Min(parentPawn.ageTracker.AgeBiologicalYears, 125) - 25) / 500f;

                            musclegainF += (float)(Math.Min(parentPawn.ageTracker.AgeBiologicalYears, 125) - 25) / 200f;
                            muscleloseF -= (float)(Math.Min(parentPawn.ageTracker.AgeBiologicalYears, 125) - 25) / 200f;
                        }
                        break;
                }

                //UI
                if (RimbodySettings.showFleck && parentPawn.IsHashIntervalTick(150))
                {

                    if (gain == (2f * MuscleMass * musclegainF) + 100f)
                    {
                        FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_MaxGain);
                    }
                    else if (strengthFactor >= 2f)
                    {
                        FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_Gain);
                    }
                    else if (strengthFactor >= 1.8f)
                    {
                        FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_GainLimited);
                    }
                    else if (cardioFactor >= 2f)
                    {
                        FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_Cardio);
                    }
                    else if (cardioFactor >= 1.8f)
                    {
                        FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_CardioLimited);
                    }
                }

                //Starving
                if (parentPawn?.needs?.food.Starving == true)
                {
                    Hediff malnutritionHediff = parentPawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Malnutrition);
                    if (malnutritionHediff != null)
                    {
                        cardioFactor += 2 * malnutritionHediff.Severity;
                        strengthFactor -= 2 * malnutritionHediff.Severity * malnutritionHediff.Severity;
                    }
                }

                //Fat
                float fatGain = Mathf.Pow(curFood, 0.5f);
                float fatLoss = (BodyFat + 60f) / (50f) * cardioFactor;
                float fatDelta = (fatGain * fatgainF - fatLoss * fatloseF) * tickRatio * RimbodySettings.rateFactor / 400f;
                newBodyFat = Mathf.Clamp(BodyFat + fatDelta, 0f, 50f);

                //Muscle
                float muscleGain = 0.05f * ((MuscleMass + 75f) / (MuscleMass - 55f) + 25f);
                float muscleLoss = (50f / (BodyFat + 50f)) * ((MuscleMass + 50f) / 125f) * Mathf.Pow(((curFood + 0.125f) / 0.125f), -0.5f);
                float muscleDelta = 0f;

                if (parentPawn.needs.rest != null)
                {
                    //Grow on sleep
                    if (forceRest || (pawnCaravan != null && (!pawnCaravan.pather.MovingNow || parentPawn.InCaravanBed() || parentPawn.CarriedByCaravan())) || (pawnCaravan == null && parentPawn.jobs?.curDriver?.asleep == true))
                    {
                        var swol = 2f;
                        var rrm = parentPawn.GetStatValue(StatDefOf.RestRateMultiplier);
                        var bre = parentPawn.CurrentBed()?.GetStatValue(StatDefOf.BedRestEffectiveness) ?? 0.8f;
                        var recoveryFactor = swol * rrm * bre;

                        if (gain - (recoveryFactor * tickRatio) > 0f)
                        {
                            gain -= recoveryFactor * tickRatio;
                            muscleDelta += recoveryFactor * tickRatio * (RimbodySettings.rateFactor / 400f);
                        }
                        else if (gain > 0f)
                        {
                            muscleDelta += (RimbodySettings.rateFactor / 400f) * gain;
                            gain = 0f;
                        }
                    }
                    //Awake
                    else
                    {
                        //Store gain
                        gain = Mathf.Clamp(gain + (strengthFactor * musclegainF * muscleGain * tickRatio), 0f, (2f * MuscleMass * musclegainF) + 100f);
                        //Grow slowly
                        var recoveryFactor = 0.2f;
                        if (gain - (recoveryFactor * tickRatio) > 0f)
                        {
                            gain -= recoveryFactor * tickRatio;
                            muscleDelta += recoveryFactor * tickRatio * (RimbodySettings.rateFactor / 400f);
                        }
                        else if (gain > 0f)
                        {
                            muscleDelta += (RimbodySettings.rateFactor / 400f) * gain;
                            gain = 0f;
                        }
                    }
                }
                //Sleepless pawns.
                else
                {
                    //Store gain
                    gain = Mathf.Clamp(gain + (strengthFactor * musclegainF * muscleGain * tickRatio), 0f, (2f * MuscleMass * musclegainF) + 100f);
                    //Grow always
                    var swol = 2f;
                    var rrm = parentPawn.GetStatValue(StatDefOf.RestRateMultiplier);
                    var recoveryFactor = swol * rrm;
                    if (gain - (recoveryFactor * tickRatio) > 0f)
                    {
                        gain -= recoveryFactor * tickRatio;
                        muscleDelta += recoveryFactor * tickRatio * (RimbodySettings.rateFactor / 400f);
                    }
                    else if (gain > 0f)
                    {
                        muscleDelta += (RimbodySettings.rateFactor / 400f) * gain;
                        gain = 0f;
                    }
                }
                muscleDelta -= muscleloseF * muscleLoss * tickRatio * RimbodySettings.rateFactor / 400f;
                newMuscleMass = Mathf.Clamp(MuscleMass + muscleDelta, 0f, 50f);
                //BodyChange Check
                if (fatDelta > 0f)
                {
                    if (BodyFat < RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod && newBodyFat >= RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                    else if (BodyFat < RimbodySettings.fatThresholdFat + RimbodySettings.gracePeriod && newBodyFat >= RimbodySettings.fatThresholdFat + fatThreshholdE + RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                }
                else if (fatDelta < 0f)
                {
                    if (BodyFat > RimbodySettings.fatThresholdThin - RimbodySettings.gracePeriod && newBodyFat <= RimbodySettings.fatThresholdThin - RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                    else if (BodyFat > RimbodySettings.fatThresholdFat - RimbodySettings.gracePeriod && newBodyFat <= RimbodySettings.fatThresholdFat + fatThreshholdE - RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                }

                if (muscleDelta > 0f)
                {
                    if (MuscleMass < RimbodySettings.muscleThresholdThin + RimbodySettings.gracePeriod && newMuscleMass >= RimbodySettings.muscleThresholdThin + RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                    else if (MuscleMass < RimbodySettings.muscleThresholdHulk + RimbodySettings.gracePeriod && newMuscleMass >= RimbodySettings.muscleThresholdHulk + RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                }
                else if (muscleDelta < 0f)
                {
                    if (MuscleMass > RimbodySettings.muscleThresholdThin - RimbodySettings.gracePeriod && newMuscleMass <= RimbodySettings.muscleThresholdThin - RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                    else if (MuscleMass > RimbodySettings.muscleThresholdHulk - RimbodySettings.gracePeriod && newMuscleMass <= RimbodySettings.muscleThresholdHulk - RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                }
                //Log.Message($"{parentPawn.Name} got past the null reference check. Adjusting with strenght: {strengthFactor}, cardio: {cardioFactor}");

                //Apply New Values
                BodyFat = newBodyFat;
                MuscleMass = newMuscleMass;

                //BodyChange
                if (checkFlag == true)
                {
                    parentPawn.story.bodyType = GetValidBody();
                    parentPawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                //Log Memory
                //string memoery_String = string.Join(", ", memory);
                //Log.Message($"{parentPawn.Name}'s memory: {memoery_String}");

            }
        }

        public void UpdateCarryweight()
        {
            float inventoryWeight = MassUtility.GearAndInventoryMass(parentPawn);
            float capacityWeight = 0f;
            for (int i = 0; i < parentPawn.carryTracker.innerContainer.Count; i++)
            {
                Thing thing = parentPawn.carryTracker.innerContainer[i];
                capacityWeight += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Mass);
            }
            carryFactor = 0.5f * Mathf.Clamp((inventoryWeight + capacityWeight) / (parentPawn.GetStatValue(StatDefOf.CarryingCapacity) + MassUtility.Capacity(parentPawn)), 0f, 1f);
        }

        public override void CompTick()
        {
            if (!parentPawn.Dead)
            {
                if (parentPawn.IsHashIntervalTick(RimbodySettings.CalcEveryTick))
                {
                    PhysiqueTick();
                }
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            PhysiqueValueSetup();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            PhysiqueValueSetup();
        }

        public void ResetBody()
        {
            if (BodyFat<0 || MuscleMass  < 0) return;

            if(parentPawn?.story?.bodyType != null)
            {
                parentPawn.story.bodyType = GetValidBody();
                parentPawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        public virtual BodyTypeDef GetValidBody()
        {
            if (ModsConfig.BiotechActive && parentPawn.DevelopmentalStage.Juvenile())
            {
                if (parentPawn.DevelopmentalStage.Baby())
                {
                    return BodyTypeDefOf.Baby;
                }

                if (parentPawn.DevelopmentalStage.Child())
                {
                    return BodyTypeDefOf.Child;
                }
            }
            var fatE = 0.0f;
            if(RimbodySettings.genderDifference && (parentPawn.gender == Gender.Female))
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
                return parentPawn.gender == Gender.Male ? BodyTypeDefOf.Male : BodyTypeDefOf.Female;
            }
            else
            {
                return parentPawn.story.bodyType;
            }
        }

        public bool IsValidBodyType()
        {
            if (parentPawn?.story?.bodyType != null)
            {
                if (parentPawn.ageTracker?.CurLifeStage?.developmentalStage != DevelopmentalStage.Adult)
                {
                    return true;
                }
                if (parentPawn.story.bodyType == BodyTypeDefOf.Fat)
                {
                    if (BodyFat > RimbodySettings.fatThresholdFat)
                    {
                        return true;
                    }
                    return false;
                }
                else if (parentPawn.story.bodyType == BodyTypeDefOf.Hulk)
                {
                    if (MuscleMass > RimbodySettings.muscleThresholdHulk && RimbodySettings.fatThresholdFat >= BodyFat)
                    {
                        return true;
                    }
                    return false;
                }
                else if (parentPawn.story.bodyType == BodyTypeDefOf.Thin)
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

        public (float, float) RandomCompPhysiqueByBodyType()
        {
            if (parentPawn?.story?.bodyType != null)
            {
                var fat = parentPawn.story.bodyType == BodyTypeDefOf.Fat ? GenMath.RoundTo(Rand.Range(RimbodySettings.fatThresholdFat, 50f), 0.01f) :
                    parentPawn.story.bodyType == BodyTypeDefOf.Hulk ? GenMath.RoundTo(Rand.Range(0f, RimbodySettings.fatThresholdFat - RimbodySettings.gracePeriod), 0.01f) :
                    parentPawn.story.bodyType == BodyTypeDefOf.Thin ? GenMath.RoundTo(Rand.Range(0f, RimbodySettings.fatThresholdThin), 0.01f) :
                    GenMath.RoundTo(Rand.Range(RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod, RimbodySettings.fatThresholdFat - RimbodySettings.gracePeriod), 0.01f);

                var muscle = parentPawn.story.bodyType == BodyTypeDefOf.Hulk ? GenMath.RoundTo(Rand.Range(RimbodySettings.muscleThresholdHulk, 50f), 0.01f) :
                    parentPawn.story.bodyType == BodyTypeDefOf.Thin ? GenMath.RoundTo(Rand.Range(0f, RimbodySettings.muscleThresholdThin), 0.01f) :
                    GenMath.RoundTo(Rand.Range(RimbodySettings.muscleThresholdThin + RimbodySettings.gracePeriod, RimbodySettings.muscleThresholdHulk - RimbodySettings.gracePeriod), 0.01f);

                return (Mathf.Clamp(fat, 0f, 50f), Mathf.Clamp(muscle, 0f, 50f));
            }
            return (-1f, -1f);
        }

        public void PhysiqueValueSetup()
        {

            if (HARCompat.Active && parentPawn != null && !HARCompat.CompatibleRace(parentPawn))
            {
                BodyFat = -2f;
                MuscleMass = -2f;
            }

            else if (parentPawn != null && (BodyFat == -1f || MuscleMass == -1f))
            {
                (BodyFat, MuscleMass) = RandomCompPhysiqueByBodyType();
            }
        }

        //Memory
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

        public override void Notify_AddBedThoughts(Pawn pawn)
        {
            base.Notify_AddBedThoughts(pawn);
            if (memory == null)
            {
                Log.Error("Rimbody found null memory. This should never happend.");
                memory = [];
            }
            else
            {
                memory.Clear();
            }
            
        }

        //Biotech
        public void ApplyGene()
        {
            if (!ModsConfig.BiotechActive || parentPawn is null)
            {
                return;
            }
            if (parentPawn.genes is null) return;

            if (parentPawn.genes.HasActiveGene(GeneBodyFat))
            {
                FatGainFactor = 1.25f;
                FatLoseFactor = 0.85f;
            }
            else if (parentPawn.genes.HasActiveGene(GeneBodyThin))
            {
                FatGainFactor = 0.75f;
                FatLoseFactor = 1.15f;
            }
            else if (parentPawn.genes.HasActiveGene(GeneBodyHulk))
            {
                MuscleGainFactor = 1.25f;
                MuscleLoseFactor = 0.85f;
            }
            else if (parentPawn.genes.HasActiveGene(GeneBodyStandard))
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

        //Scribe
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
            Scribe_Values.Look(ref carryFactor, "Physique_carryFactor", 0f);
            var wo_memory_tmp = new List<string>();
            while (memory.Any()) wo_memory_tmp.Add(memory.Dequeue());
            Scribe_Collections.Look(ref wo_memory_tmp, "Physique_memory");
            memory = new Queue<string>();
            if (wo_memory_tmp!=null)
            {
                foreach (var mem in wo_memory_tmp) memory.Enqueue(mem);
            }
            
        }

    }


}
