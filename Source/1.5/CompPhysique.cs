using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class CompPhysique : ThingComp
    {

        //Strength
        private static readonly float fatiguelaborS = 1.4f;
        //private static readonly float _workoutS = 2.0f; // [Strength]
        //private static readonly float _hardworkS = 1.2f; // [Melee], [Balance], [HardLabor]
        //private static readonly float _workS = 0.8f; // [NormalLabor]
        //private static readonly float _lightworkS = 0.4f; // [LightLabor]
        private static readonly float _sprintS = 0.25f; // LocomotionUrgency.Sprint, [Cardio]
        private static readonly float _movingS = 0.2f; // LocomotionUrgency.Walk, LocomotionUrgency.Jog
        //private static readonly float _activityS = 0.2f; //[Activity]
        private static readonly float _ambleS = 0.15f; // LocomotionUrgency.Amble
        private static readonly float _baseS = 0.1f; // [Base]
        private static readonly float _lyingS = 0.0f; // [Rest]

        //Cardio
        private static readonly float _workoutC = 2.0f; // [Cardio]
        private static readonly float _sprintC = 1.9f; // LocomotionUrgency.Sprint
        private static readonly float _joggingC = 0.8f; // LocomotionUrgency.Jog
        //private static readonly float _hardworkC = 0.6f; // [HardLabor], [Melee]
        //private static readonly float _workC = 0.5f; // [NormalLabor], [Strength Workout]
        private static readonly float _walkingC = 0.4f; // LocomotionUrgency.Walk, [Light Labor]
        private static readonly float _ambleC = 0.35f; // LocomotionUrgency.Amble, [Activity]
        private static readonly float _baseC = 0.3f; // [Base]
        private static readonly float _lyingC = 0.2f; // [Rest]

        //Internals
        private Pawn parentPawnInt = null;
        private bool? isJoggerInt;
        public bool PostGen = false;
        private Pawn parentPawn
        {
            get
            {
                parentPawnInt ??= parent as Pawn;
                return parentPawnInt;
            }
        }
        public float? breInt;
        public float bre
        {
            get
            {
                if (breInt is null)
                {
                    var bed = parentPawn.CurrentBed();
                    breInt = ((bed == null || !bed.def.statBases.StatListContains(StatDefOf.BedRestEffectiveness)) ? StatDefOf.BedRestEffectiveness.valueIfMissing : bed.GetStatValue(StatDefOf.BedRestEffectiveness));
                }
                return breInt ?? StatDefOf.BedRestEffectiveness.valueIfMissing;
            }
        }

        //Job Overrides
        public bool forceRest = false; //For mod compatibility.
        public bool jobOverride = false;
        public float strengthOverride = 0f;
        public float cardioOverride = 0f;
        public float memoryFactorOverride = 1f;
        public List<float> partsOverride = null;

        //Fat
        public float BodyFat = -1f;
        public float FatGainFactor = 1f;
        public float FatLoseFactor = 1f;
        public bool useFatgoal = false;
        public float FatGoal = 25f;

        //Muscle
        public float MuscleMass = -1f;
        public float MuscleGainFactor = 1f;
        public float MuscleLoseFactor = 1f;
        public bool useMuscleGoal = false;
        public float MuscleGoal = 25f;

        //Gain
        public float gain = 0f;
        public float gainF = 1f;
        public float gainMax => (2f * MuscleMass * gainF) + 100f;

        //Fatigue and Exhuastion
        public float exhaustion = 0f;
        public bool resting = false;
        public List<float> partFatigue = Enumerable.Repeat(0f, RimbodySettings.PartCount).ToList();

        //Traits and Genes
        public bool isNonSen = false;
        public bool isJogger
        {
            get
            {
                if (isJoggerInt is null)
                {
                    if (parentPawn?.story?.traits?.HasTrait(SpeedOffsetDef, 2) == true)
                    {
                        isJoggerInt = true;
                    }
                    else
                    {
                        isJoggerInt = false;
                    }
                }
                return isJoggerInt ?? false;
            }
        }
        //public bool isJogger => (parentPawn?.story?.traits?.HasTrait(SpeedOffsetDef, 2) == true);

        //Memory
        public Queue<string> memory = [];
        public string lastMemory = string.Empty;
        public int lastWorkoutTick = 0;
        public float carryFactor = 0f;

        public CompProperties_Physique Props => (CompProperties_Physique)props;

        private static readonly TraitDef SpeedOffsetDef = DefDatabase<TraitDef>.GetNamed("SpeedOffset", true);
        private static readonly GeneDef GeneBodyFat = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Fat", true) : null;
        private static readonly GeneDef GeneBodyThin = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Thin", true) : null;
        private static readonly GeneDef GeneBodyHulk = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Hulk", true) : null;
        private static readonly GeneDef GeneBodyStandard = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("Body_Standard", true) : null;
        private static readonly GeneDef NonSenescent = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("DiseaseFree", true) : null;

        private CompHoldingPlatformTarget platformComp;
        private CompHoldingPlatformTarget PlatformTarget => platformComp ?? (platformComp = parentPawn.TryGetComp<CompHoldingPlatformTarget>());

        private void PhysiqueTick()
        {

            if (Rimbody_Utility.isColonyMember(parentPawn) || parentPawn.IsPrisonerOfColony)
            {
                if (!Rimbody_Utility.shouldTick(parentPawn))
                {
                    return;
                }
                if (parentPawn.Deathresting || parentPawn.Suspended)
                {
                    return;
                }
                if (ModsConfig.AnomalyActive)
                {
                    if (PlatformTarget?.CurrentlyHeldOnPlatform ?? false)
                    {
                        return;
                    }
                }

                //Set up
                var tickRatio = RimbodySettings.CalcEveryTick / 150f;
                var customRatio = RimbodySettings.rateFactor * 0.0025f;
                var curFood = Mathf.Clamp(parentPawn.needs.food.CurLevelPercentage, 0f, 1f);
                var curJobDef = parentPawn.CurJobDef;
                var curDriver = parentPawn.jobs?.curDriver;
                bool restingCheck = false;

                var pawnCaravan = parentPawn.GetCaravan();

                float newBodyFat;
                float newMuscleMass;

                bool doingS = false;
                bool doingB = false;
                bool doingC = false;
                int UIflag = 0;

                float strengthFactor = _baseS; //0.1f
                float cardioFactor = _baseC; //0.3f
                List<float> partsToApplyFatigue = null;

                float appliedEfficiency = 1;

                //Factor Calculation
                //If factor is Forced
                var harmonyCheckstring = HarmonyCheck();
                if (harmonyCheckstring != string.Empty)
                {
                    (strengthFactor, cardioFactor, partsToApplyFatigue) = HarmonyValues(harmonyCheckstring);
                }
                else if (forceRest)
                {
                    strengthFactor = _lyingS; //0.0f
                    cardioFactor = _lyingC; //0.2f
                }
                //Get factors from dedicated Rimbody workout jobs
                else if (jobOverride)
                {
                    strengthFactor = strengthOverride * memoryFactorOverride;
                    cardioFactor = cardioOverride;
                    partsToApplyFatigue = partsOverride;
                    appliedEfficiency = memoryFactorOverride;
                    switch (curJobDef.defName)
                    {
                        case string job when RimbodyDefLists.StrengthJob.Contains(job):
                            doingS = true;
                            UIflag = 2;
                            strengthFactor = strengthFactor * RimbodySettings.WorkOutGainEfficiency;
                            break;
                        case string job when RimbodyDefLists.BalanceJob.Contains(job):
                            doingB = true;
                            UIflag = 2;
                            break;
                        case string job when RimbodyDefLists.CardioJob.Contains(job):
                            doingC = true;
                            UIflag = 2;
                            break;
                    }
                }
                //Factors based on current jobs
                else
                {
                    //Factors for Caravan
                    if (pawnCaravan != null)
                    {
                        //Resting
                        if (!pawnCaravan.pather.MovingNow || parentPawn.InCaravanBed() || parentPawn.CarriedByCaravan())
                        {
                            strengthFactor = _lyingS; //0.0f
                            cardioFactor = _lyingC; //0.2f
                        }
                        //Moving, but not boarded
                        else if (pawnCaravan.pather.MovingNow)
                        {
                            strengthFactor = _movingS; //0.2f
                            cardioFactor = _walkingC; //0.4f
                        }
                    }
                    else if (curJobDef != null && curDriver != null)
                    {
                        //Special cases: moving
                        if (parentPawn.pather?.MovingNow == true)
                        {
                            switch (parentPawn.jobs.curJob.locomotionUrgency)
                            {
                                case LocomotionUrgency.Sprint:
                                    {
                                        strengthFactor = _sprintS; //0.25f
                                        cardioFactor = _sprintC; //1.9f
                                        //Jogging
                                        doingC = true;
                                        if (curJobDef?.defName == "Rimbody_Jogging" || curJobDef?.defName == "NatureRunning")
                                        {
                                            UIflag = 2;
                                        }
                                        if (isJogger)
                                        {
                                            cardioFactor = _workoutC; //2.0f
                                            partsToApplyFatigue = RimbodyDefLists.jogging_parts_jogger;
                                        }
                                        else
                                        {
                                            partsToApplyFatigue = RimbodyDefLists.jogging_parts;
                                        }
                                    }
                                    break;
                                case LocomotionUrgency.Jog:
                                    {
                                        strengthFactor = _movingS; //0.2f
                                        cardioFactor = _joggingC; //0.8f
                                    }
                                    break;
                                case LocomotionUrgency.Walk:
                                    {
                                        strengthFactor = _movingS; //0.2f
                                        cardioFactor = _walkingC; //0.4f
                                    }
                                    break;
                                default:
                                    {
                                        strengthFactor = _ambleS; //0.15f
                                        cardioFactor = _ambleC; //0.35f
                                    }
                                    break;
                            }
                            strengthFactor += carryFactor * RimbodySettings.carryRateMultiplier;
                        }
                        //Special cases: Lying down
                        else if (curDriver?.CurToilString == "LayDown")
                        {
                            strengthFactor = _lyingS; //0.0f
                            cardioFactor = _lyingC; //0.2f
                        }
                        else
                        {
                            //get work factor
                            if (RimbodyDefLists.JobExtensionCache.TryGetValue(curJobDef.defName, out var jobExtension))
                            {
                                if (jobExtension.JobCategory != RimbodyJobCategory.None)
                                {
                                    (strengthFactor, cardioFactor, partsToApplyFatigue) = GetFactor(jobExtension.JobCategory);
                                }
                                else
                                {
                                    strengthFactor = jobExtension.strength;
                                    cardioFactor = jobExtension.cardio;
                                    partsToApplyFatigue = jobExtension.strengthParts;
                                }
                                if(partsToApplyFatigue != null) //Parts is needed to be treated as anything other than job.
                                {
                                    switch (jobExtension.TreatAs)
                                    {
                                        case RimbodyWorkoutCategory.Job:
                                            break;
                                        case RimbodyWorkoutCategory.Strength:
                                            doingS = true;
                                            UIflag = 2;
                                            strengthFactor *= RimbodySettings.WorkOutGainEfficiency;
                                            break;
                                        case RimbodyWorkoutCategory.Balance:
                                            doingB = true;
                                            UIflag = 2;
                                            break;
                                        case RimbodyWorkoutCategory.Cardio:
                                            doingC = true;
                                            UIflag = 2;
                                            break;
                                    }
                                }
                            }
                            else if (parentPawn?.CurJob?.workGiverDef is { } workGiverDef)
                            {
                                if (RimbodyDefLists.GiverExtensionCache.TryGetValue(workGiverDef.defName, out var giverExtension))
                                {
                                    if (giverExtension.JobCategory != RimbodyJobCategory.None)
                                    {
                                        (strengthFactor, cardioFactor, partsToApplyFatigue) = GetFactor(giverExtension.JobCategory);
                                    }
                                    else
                                    {
                                        strengthFactor = giverExtension.strength;
                                        cardioFactor = giverExtension.cardio;
                                        partsToApplyFatigue = giverExtension.strengthParts;
                                    }
                                    if (partsToApplyFatigue != null) //Parts is needed to be treated as anything other than job.
                                    {
                                        switch (giverExtension.TreatAs)
                                        {
                                            case RimbodyWorkoutCategory.Job:
                                                break;
                                            case RimbodyWorkoutCategory.Strength:
                                                doingS = true;
                                                UIflag = 2;
                                                strengthFactor *= RimbodySettings.WorkOutGainEfficiency;
                                                break;
                                            case RimbodyWorkoutCategory.Balance:
                                                doingB = true;
                                                UIflag = 2;
                                                break;
                                            case RimbodyWorkoutCategory.Cardio:
                                                doingC = true;
                                                UIflag = 2;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                appliedEfficiency *= ApplyFatigueToFactors(partsToApplyFatigue, ref strengthFactor, ref cardioFactor, (float)RimbodySettings.CalcEveryTick * 0.001f); //Apply partFatigue. Raises (partfactor * 0.001) per tick (partfactor * 2.5 per hour)
                //Decide on the Fleck
                if (doingS)
                {
                    if (appliedEfficiency <= 0.9)
                    {
                        UIflag--;
                    }
                }
                else if (doingC)
                {
                    if (cardioFactor <= 1.8)
                    {
                        UIflag--;
                    }
                }
                //Dev Logging
                //if (RimbodySettings.LogPhysique)
                //{
                //    Log.Message($"{parentPawn.Name} doing {curJobDef.defName}. doingS: {doingS}, doingC: {doingC}. jobOverride: {jobOverride}, memoryFactorOverride: {memoryFactorOverride}, appliedEfficiency: {appliedEfficiency}. Applied Factors: s:{strengthFactor}, c:{cardioFactor} with parts: {partsToApplyFatigue != null}");
                //}
                //if (RimbodySettings.LogMemory)
                //{
                //    string memoery_String = string.Join(", ", memory);
                //    Log.Message($"{parentPawn.Name}'s memory: {memoery_String}");
                //}

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

                //Gender
                if (RimbodySettings.genderDifference == true)
                {
                    if (parentPawn.gender == Gender.Male)
                    {
                        musclegainF += RimbodySettings.maleMusclegain;
                    }
                    else
                    {
                        musclegainF -= RimbodySettings.maleMusclegain;
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
                        if (isNonSen)
                        {
                            var agepoint = (float)(25 - Math.Min(parentPawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint));
                            fatgainF -= agepoint * 0.002f;
                            fatloseF += agepoint * 0.002f;

                            musclegainF += agepoint * 0.005f;
                            muscleloseF -= agepoint * 0.005f;
                        }
                        else
                        {
                            var agepoint = (float)(25 - Math.Min(parentPawn.ageTracker.AgeBiologicalYears, 125));
                            fatgainF -= agepoint * 0.002f;
                            fatloseF += agepoint * 0.002f;

                            musclegainF += agepoint * 0.005f;
                            muscleloseF -= agepoint * 0.005f;
                        }
                        break;
                }
                gainF = musclegainF;

                //UI
                if (RimbodySettings.showFleck && UIflag>0 && parentPawn.IsHashIntervalTick(150))
                {
                    if (doingS)
                    {
                        if (gain == gainMax)
                        {
                            FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_MaxGain);
                        }
                        else if (UIflag < 2)
                        {
                            FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_GainLimited);
                        }
                        else
                        {
                            FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_Gain);
                        }
                    }
                    else if (doingC)
                    {
                        if (UIflag < 2)
                        {
                            FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_CardioLimited);
                        }
                        else
                        {
                            FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_Cardio);
                        }
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
                float fatLoss = (BodyFat + 43f) * 0.025f * cardioFactor; //float fatLoss = (BodyFat + 60f) / (50f) * cardioFactor;
                float fatDelta = (fatGain * fatgainF - fatLoss * fatloseF) * tickRatio * customRatio;
                newBodyFat = Mathf.Clamp(BodyFat + fatDelta, 0f, 50f);

                //Muscle
                float muscleGain = 0.045f * ((MuscleMass + 75f) / (MuscleMass - 55f) + 25f);
                float muscleLoss = (51.5f / (BodyFat + 50f)) * ((MuscleMass + 25f) * 0.008f) * Mathf.Pow(((curFood + 0.125f) * 8f), -0.5f);//(51.5f / (BodyFat + 50f)) * ((MuscleMass + 50f) / 125f) * Mathf.Pow(((curFood + 0.125f) / 0.125f), -0.5f);
                float muscleDelta = 0f;

                //exhaustion recovery
                float exhaustionDelta = 0f;
                if (RimbodySettings.useExhaustion)
                {
                    exhaustionDelta = -RimbodySettings.CalcEveryTick * (0.7f - (7f * (BodyFat) / 100f * (BodyFat - 50f) / 100f * (BodyFat + 100f) / 100f)) * ((MuscleMass / 50) + 1) / 1000f;
                }
                

                if (parentPawn.needs.rest != null)
                {
                    //Grow on sleep
                    if (forceRest || parentPawn.needs.rest?.Resting == true)
                    {
                        restingCheck = true;
                        var swol = 2f;
                        var rrm = parentPawn.GetStatValue(StatDefOf.RestRateMultiplier);
                        var recoveryFactor = Mathf.Max( swol * rrm * bre, 0.2f);

                        if (gain - (recoveryFactor * tickRatio) > 0f)
                        {
                            gain -= recoveryFactor * tickRatio;
                            muscleDelta += recoveryFactor * tickRatio * customRatio;
                        }
                        else if (gain > 0f)
                        {
                            muscleDelta += customRatio * gain;
                            gain = 0f;
                        }
                        exhaustionDelta = 8f * exhaustionDelta;
                    }
                    //Awake
                    else
                    {
                        breInt = null;
                        //Store gain
                        gain = Mathf.Clamp(gain + (strengthFactor * musclegainF * muscleGain * tickRatio), 0f, gainMax);
                        //Grow slowly
                        var recoveryFactor = 0.2f;
                        if (gain - (recoveryFactor * tickRatio) > 0f)
                        {
                            gain -= recoveryFactor * tickRatio;
                            muscleDelta += recoveryFactor * tickRatio * customRatio;
                        }
                        else if (gain > 0f)
                        {
                            muscleDelta += customRatio * gain;
                            gain = 0f;
                        }
                    }
                }
                //Sleepless pawns.
                else
                {
                    restingCheck = true;
                    //Store gain
                    gain = Mathf.Clamp(gain + (strengthFactor * musclegainF * muscleGain * tickRatio), 0f, gainMax);
                    //Grow always
                    var swol = 2f;
                    var rrm = parentPawn.GetStatValue(StatDefOf.RestRateMultiplier);
                    var recoveryFactor = swol * rrm;
                    if (gain - (recoveryFactor * tickRatio) > 0f)
                    {
                        gain -= recoveryFactor * tickRatio;
                        muscleDelta += recoveryFactor * tickRatio * customRatio;
                    }
                    else if (gain > 0f)
                    {
                        muscleDelta += customRatio * gain;
                        gain = 0f;
                    }
                    exhaustionDelta = 4f * exhaustionDelta;
                }
                muscleDelta -= muscleloseF * muscleLoss * tickRatio * customRatio;
                newMuscleMass = Mathf.Clamp(MuscleMass + muscleDelta, 0f, 50f);
                //BodyChange Check
                bool checkFlag = shouldCheckBody(fatDelta, muscleDelta, newBodyFat, newMuscleMass);

                //Manage fatigue and exhaustion
                if (RimbodySettings.useFatigue)
                {
                    if (partsToApplyFatigue == null)
                    {
                        RestorePartFatigue(restingCheck ? RimbodySettings.CalcEveryTick * 0.00015f : RimbodySettings.CalcEveryTick * 0.00005f); //restore 1 / 20000 per tick. triple when resting. A day is 60000 ticks, so awake, a pawn can restore 3 points per day. if asleep for 7H during a day, 5point total
                        
                    }
                }
                if (RimbodySettings.useExhaustion)
                {
                    if (doingS)
                    {
                        exhaustionDelta = RimbodySettings.CalcEveryTick / (25f * (0.5f + (5f * (MuscleMass / 100f)) + (4f * (BodyFat / 100f) * (MuscleMass / 100f)) + (2f * (BodyFat / 100f))));
                    }
                    else if (doingB)
                    {
                        exhaustionDelta = RimbodySettings.CalcEveryTick / (25f * (1f + (70f * (BodyFat) / 100f * (BodyFat - 50f) / 100f * (BodyFat - 100f) / 100f)) * (1f - ((5f * (MuscleMass / 100f) * (MuscleMass - 25f - RimbodySettings.muscleThresholdHulk)) / 100f)));
                    }
                    else if (doingC)
                    {
                        exhaustionDelta = RimbodySettings.CalcEveryTick / (25f * (1f + (70f * (BodyFat) / 100f * (BodyFat - 50f) / 100f * (BodyFat - 100f) / 100f)) * (1f - ((5f * (MuscleMass / 100f) * (MuscleMass - 25f - RimbodySettings.muscleThresholdHulk)) / 100f)));
                    }
                    var newExhaustion = exhaustion + exhaustionDelta;
                    if (newExhaustion >= 100f)
                    {
                        resting = true;
                        exhaustion = 100f;
                    }
                    else
                    {
                        exhaustion = Mathf.Max(0f, newExhaustion);
                    }

                    if (exhaustion < 40f && resting)
                    {
                        resting = false;
                    }

                }


                //Apply New Values
                BodyFat = newBodyFat;
                MuscleMass = newMuscleMass;

                //BodyChange
                if (checkFlag == true)
                {
                    parentPawn.story.bodyType = GetValidBody();
                    parentPawn.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
        }

        //Harmony hook for mod compatibility.
        public string HarmonyCheck()
        {
            return string.Empty;
        }
        public (float, float, List<float>) HarmonyValues(string harmonyKey)
        {        
            return (0f, 0f, null); //(strengthHarmony, cardioOHarmony, partsHarmony);
        }

        //Utilities
        public void DirtyTraitCache()
        {
            isJoggerInt = null;
        }

        public bool shouldCheckBody(float fatDelta, float muscleDelta, float newBodyFat, float newMuscleMass)
        {
            float fatThreshholdE = 0f;
            if (RimbodySettings.genderDifference == true && parentPawn.gender == Gender.Female)
            {
                fatThreshholdE = RimbodySettings.femaleFatThreshold;
            }

            if (fatDelta > 0f)
            {
                if (BodyFat < RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod && newBodyFat >= RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod)
                {
                    return true;
                }
                else if (BodyFat < RimbodySettings.fatThresholdFat + RimbodySettings.gracePeriod && newBodyFat >= RimbodySettings.fatThresholdFat + fatThreshholdE + RimbodySettings.gracePeriod)
                {
                    return true;
                }
            }
            else if (fatDelta < 0f)
            {
                if (BodyFat > RimbodySettings.fatThresholdThin - RimbodySettings.gracePeriod && newBodyFat <= RimbodySettings.fatThresholdThin - RimbodySettings.gracePeriod)
                {
                    return true;
                }
                else if (BodyFat > RimbodySettings.fatThresholdFat - RimbodySettings.gracePeriod && newBodyFat <= RimbodySettings.fatThresholdFat + fatThreshholdE - RimbodySettings.gracePeriod)
                {
                    return true;
                }
            }

            if (muscleDelta > 0f)
            {
                if (MuscleMass < RimbodySettings.muscleThresholdThin + RimbodySettings.gracePeriod && newMuscleMass >= RimbodySettings.muscleThresholdThin + RimbodySettings.gracePeriod)
                {
                    return true;
                }
                else if (MuscleMass < RimbodySettings.muscleThresholdHulk + RimbodySettings.gracePeriod && newMuscleMass >= RimbodySettings.muscleThresholdHulk + RimbodySettings.gracePeriod)
                {
                    return true;
                }
            }
            else if (muscleDelta < 0f)
            {
                if (MuscleMass > RimbodySettings.muscleThresholdThin - RimbodySettings.gracePeriod && newMuscleMass <= RimbodySettings.muscleThresholdThin - RimbodySettings.gracePeriod)
                {
                    return true;
                }
                else if (MuscleMass > RimbodySettings.muscleThresholdHulk - RimbodySettings.gracePeriod && newMuscleMass <= RimbodySettings.muscleThresholdHulk - RimbodySettings.gracePeriod)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateCarryweight()
        {
            var pawnInventoryCapacity = Rimbody_Utility.GetBaseInventoryCapacity(parentPawn);
            float inventoryWeight = MassUtility.GearAndInventoryMass(parentPawn);

            float capacityWeight = 0f;
            for (int i = 0; i < parentPawn.carryTracker.innerContainer.Count; i++)
            {
                Thing thing = parentPawn.carryTracker.innerContainer[i];
                capacityWeight += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Mass);
            }

            carryFactor = 0.5f * Mathf.Clamp((inventoryWeight + capacityWeight) / pawnInventoryCapacity, 0f, 2f);
            //Carry capacity is all about the numer of items that can be held in the innerContainer (volume). We assume that has nothing to do with strength.
            //Inventory Cacpacity, on the other hand has to do with mass.
            //We assume if the capcity is how much the pawn is comfortable carrying around on their back.

            //If inventory + held item 's weight == inventory capacity, strength factor will be 0.5, just a bit over light labor.
            //If inventory + held item 's weight > inventory capacity*2, strenght factor will be 1, a little less than hard labor.
            //Log.Message($"{parentPawn.Name}'s inventoryWeight: {inventoryWeight} capacityWeight: {capacityWeight} / pawnInventoryCapacity: {Rimbody_Utility.GetBaseInventoryCapacity(parentPawn)}. FINAL: {carryFactor}");
        }

        public override void CompTick()
        {
            if (BodyFat <= -1f || MuscleMass <= -1f)
            {
                return;
            }

            if (!parentPawn.Dead)
            {
                if (parentPawn.IsHashIntervalTick(RimbodySettings.CalcEveryTick))
                {
                    if(parentPawn.needs?.food != null)
                    {
                        PhysiqueTick();
                    }                    
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

        public void PhysiqueValueSetup(bool reset = false)
        {
            if (parentPawn != null && ((BodyFat == -1f || MuscleMass == -1f) || reset))
            {
                (BodyFat, MuscleMass) = RandomCompPhysiqueByBodyType();
                ApplyGene();
            }
        }

        //Memory
        public void AddNewMemory(String workout)
        {
            this.lastMemory = workout;
            if (this.memory.Count >= 5)
            {
                memory.Dequeue();
            }
            memory.Enqueue(workout);
        }

        //partFatigue
        public void AddPartFatigue(List<float> strengthParts, float multiplier = 1f)
        {
            if (strengthParts?.Count == RimbodySettings.PartCount && partFatigue?.Count == RimbodySettings.PartCount)
            {
                for (int i = 0; i < RimbodySettings.PartCount; i++)
                {
                    partFatigue[i] = Mathf.Clamp((strengthParts[i] * multiplier) + partFatigue[i], 0f, RimbodySettings.MaxFatiguePerPart);
                }
            }
        }
        public void RestorePartFatigue(float multiplier = 1f)
        {
            if (partFatigue?.Count == RimbodySettings.PartCount)
            {
                for (int i = 0; i < RimbodySettings.PartCount; i++)
                {
                    partFatigue[i] = Math.Max(0f, partFatigue[i] - multiplier);
                }
            }
        }
        float ApplyFatigueToFactors(List<float> strengthParts, ref float strength, ref float cardio, float addFatigueMult = 0f)
        {
            if (!RimbodySettings.useFatigue)
            {
                return 1f;
            }
            if (partFatigue == null || !(strengthParts?.Count == RimbodySettings.PartCount)) return 1f;
            float fatigueFactor = 0f;
            float total = 0;
            float spread = 0f;
            float peak = 0f;
            for (int i = 0; i < RimbodySettings.PartCount; i++)
            {
                if (strengthParts[i] > 0)
                {
                    fatigueFactor += strengthParts[i] * (10f - partFatigue[i]) * 0.1f;
                    spread = spread + Math.Min(1f, strengthParts[i]);
                    total = total + strengthParts[i];
                    peak = Math.Max(peak, strengthParts[i]);
                    partFatigue[i] = Mathf.Clamp((strengthParts[i] * addFatigueMult) + partFatigue[i], 0f, RimbodySettings.MaxFatiguePerPart);
                }
            }
            fatigueFactor = fatigueFactor / total;
            float fi = (total + ((0.1f * ((float)RimbodySettings.PartCount - spread)) * peak)) * 0.4f;
            strength = strength * (0.25f + (0.75f * fatigueFactor)) * fi;
            //cardio = cardio //Cardio is always fixed.
            return 1.35f * fatigueFactor;
        }

        public float GetStrengthJobScore(List<float> strengthParts, float strength)
        {
            if (!RimbodySettings.useFatigue)
            {
                return strength;
            }
            if (partFatigue == null || strengthParts.Count != RimbodySettings.PartCount) return 0;
            float fatigueFactor = 0f;
            float total = 0;
            float spread = 0f;
            float peak = 0f;
            for (int i = 0; i < RimbodySettings.PartCount; i++)
            {
                if (strengthParts[i] > 0)
                {
                    fatigueFactor += strengthParts[i] * (10f - partFatigue[i]) * 0.1f;
                    spread = spread + Math.Min(1f, strengthParts[i]);
                    total = total + strengthParts[i];
                    peak = Math.Max(peak, strengthParts[i]);
                }
            }
            fatigueFactor = fatigueFactor / total;
            float fi = (total + ((0.1f * ((float)RimbodySettings.PartCount - spread)) * peak)) * 0.4f;
            return strength * (0.25f+(0.75f*fatigueFactor)) * fi;
        }

        public float GetBalanceJobScore(List<float> strengthParts, float strength)
        {
            if (!RimbodySettings.useFatigue)
            {
                return strength;
            }
            if (partFatigue == null || strengthParts.Count != RimbodySettings.PartCount) return 0;
            float fatigueFactor = 0f;
            float total = 0;
            float spread = 0f;
            float peak = 0f;
            for (int i = 0; i < RimbodySettings.PartCount; i++)
            {
                if (strengthParts[i] > 0)
                {
                    fatigueFactor += strengthParts[i] * (10f - partFatigue[i]) * 0.1f;
                    spread = spread + Math.Min(1f, strengthParts[i]);
                    total = total + strengthParts[i];
                    peak = Math.Max(peak, strengthParts[i]);
                }
            }
            fatigueFactor = fatigueFactor / total;
            float fi = (total + ((0.1f * ((float)RimbodySettings.PartCount - spread)) * peak)) * 0.4f;
            return strength * (0.25f + (0.75f * fatigueFactor)) * fi;
        }

        public float GetCardioJobScore(List<float> strengthParts, float cardio)
        {
            if (!RimbodySettings.useFatigue)
            {
                return cardio;
            }
            if (partFatigue == null || strengthParts.Count != RimbodySettings.PartCount) return 0f;
            float fatigueDet = 0f;
            for (int i = 0; i < RimbodySettings.PartCount; i++)
            {
                if (strengthParts[i] > 0)
                {
                    fatigueDet += strengthParts[i] * (1f + partFatigue[i]);
                }
            }
            fatigueDet = 90f - fatigueDet;
            return cardio * fatigueDet;
        }

        public float GetWorkoutScore(RimbodyWorkoutCategory category, WorkOut workout)
        {
            switch (category)
            {
                case RimbodyWorkoutCategory.Strength:
                    if(workout.strengthParts == null)
                    {
                        return 0f;
                    }
                    return  GetStrengthJobScore(workout.strengthParts, workout.strength);
                case RimbodyWorkoutCategory.Cardio:
                    if (workout.strengthParts == null)
                    {
                        return 0f;
                    }
                    return GetCardioJobScore(workout.strengthParts, workout.cardio);
                case RimbodyWorkoutCategory.Balance:
                    if (workout.strengthParts == null)
                    {
                        return 0f;
                    }
                    return GetBalanceJobScore(workout.strengthParts, workout.strength);
            }
            return 0;
        }

        //Factor
        public static (float, float, List<float>) GetFactor(RimbodyJobCategory jobCategory)
        {
            if (RimbodySettings.useFatigue)
            {
                return jobCategory switch
                {
                    //Strength, Cardio, Part
                    RimbodyJobCategory.None => (0.1f, 0.3f, null),
                    RimbodyJobCategory.Melee => (fatiguelaborS, 0.6f, [0.5f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f, 0.1f, 0.1f, 0.1f]),
                    RimbodyJobCategory.HardLabor => (fatiguelaborS, 0.6f, [0.4f, 0.3f, 0.4f, 0.3f, 0.2f, 0.2f, 0.2f, 0.2f, 0.1f]),
                    RimbodyJobCategory.NormalLabor => (fatiguelaborS, 0.5f, [0.2f, 0.2f, 0.3f, 0.2f, 0.2f, 0.2f, 0.1f, 0.1f, 0.1f]),
                    RimbodyJobCategory.LightLabor => (fatiguelaborS, 0.4f, [0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f]),
                    RimbodyJobCategory.Activity => (0.2f, 0.35f, null),
                    RimbodyJobCategory.Base => (0.1f, 0.3f, null),
                    RimbodyJobCategory.Rest => (0f, 0.2f, null),
                    _ => (0.1f, 0.3f, null)
                };
            }
            else
            {
                return jobCategory switch
                {
                    //Strength, Cardio, Part
                    RimbodyJobCategory.None => (0.1f, 0.3f, null),
                    RimbodyJobCategory.Melee => (1.2f, 0.6f, null),
                    RimbodyJobCategory.HardLabor => (1.2f, 0.6f, null),
                    RimbodyJobCategory.NormalLabor => (0.8f, 0.5f, null),
                    RimbodyJobCategory.LightLabor => (0.4f, 0.4f, null),
                    RimbodyJobCategory.Activity => (0.2f, 0.35f, null),
                    RimbodyJobCategory.Base => (0.1f, 0.3f, null),
                    RimbodyJobCategory.Rest => (0f, 0.2f, null),
                    _ => (0.1f, 0.3f, null)
                };
            }
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
            lastMemory = string.Empty;
            
        }

        //Biotech
        public void ApplyGene()
        {
            if (!ModsConfig.BiotechActive || parentPawn is null)
            {
                return;
            }
            if (parentPawn.genes is null) return;
            
            if (parentPawn.genes.HasActiveGene(NonSenescent))
            {
                isNonSen = true;
            }
            else
            {
                isNonSen= false;
            }

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


            Scribe_Values.Look(ref jobOverride, "Physique_jobOverride", false);
            Scribe_Values.Look(ref cardioOverride, "Physique_cardioOverride", 0f);
            Scribe_Values.Look(ref strengthOverride, "Physique_strengthOverride", 0f);
            Scribe_Values.Look(ref memoryFactorOverride, "Physique_memoryFactorOverride", 1f); 
            Scribe_Collections.Look(ref partsOverride, "Physique_partsOverride", LookMode.Value);
            if (partsOverride == null || partsOverride.Count != RimbodySettings.PartCount)
            {
                partsOverride = Enumerable.Repeat(0f, RimbodySettings.PartCount).ToList();
            }

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

            Scribe_Values.Look(ref isNonSen, "Physique_isNonSen", parentPawn.genes != null ? parentPawn.genes.HasActiveGene(NonSenescent) : false);

            Scribe_Values.Look(ref gain, "Physique_gain", 0f);
            Scribe_Values.Look(ref exhaustion, "Physique_exhaustion", 0f);
            Scribe_Values.Look(ref resting, "Physique_resting", false);
            Scribe_Collections.Look(ref partFatigue, "Physique_partFatigue", LookMode.Value);
            if (partFatigue == null || partFatigue.Count != RimbodySettings.PartCount)
            {
                partFatigue = Enumerable.Repeat(0f, RimbodySettings.PartCount).ToList();
            }
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
