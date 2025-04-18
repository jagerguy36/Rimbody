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

        public bool jobOverride = false;
        public bool limitOverride = false;
        public float cardioOverride = 0f;
        public float strengthOverride = 0f;
        public int durationOverride = 0;
        public List<float> fatigueOverride = null;

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
        public float gainF = 1f;
        public float gainMax => (2f * MuscleMass * gainF) + 100f;

        public float gain = 0f;
        public float exhaustion = 0f;
        public bool resting = false;
        public bool isNonSen = false;

        public string lastMemory = string.Empty;
        public int lastWorkoutTick = 0;
        public float carryFactor = 0f;

        public Queue<string> memory = [];

        public List<float> partFatigue = Enumerable.Repeat(0f, RimbodySettings.PartCount).ToList();

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
            "Rimbody_DoStrengthLifting"
        ];

        private Pawn parentPawn
        {
            get
            {
                parentPawnInt ??= parent as Pawn;
                return parentPawnInt;
            }
        }

        private CompHoldingPlatformTarget platformComp;
        private CompHoldingPlatformTarget PlatformTarget => platformComp ?? (platformComp = parentPawn.TryGetComp<CompHoldingPlatformTarget>());

        private bool isColonyMember(Pawn pawn)
        {
            if (pawn.Faction != null && pawn.Faction.IsPlayer && pawn.RaceProps.Humanlike && !pawn.IsMutant) //The same as isColonist Check minus the slave check
            {
                return true;
            }
            return false;
        }

        private static bool shouldTick(Pawn pawn)
        {
            if (pawn.SpawnedOrAnyParentSpawned || pawn.IsCaravanMember() || PawnUtility.IsTravelingInTransportPodWorldObject(pawn))
            {
                return true;
            }
            return false;
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

            if (isColonyMember(parentPawn) || parentPawn.IsPrisonerOfColony)
            {
                if (!shouldTick(parentPawn))
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

                var tickRatio = RimbodySettings.CalcEveryTick / 150f;
                var curFood = Mathf.Clamp(parentPawn.needs.food.CurLevelPercentage, 0f, 1f);
                var curJobDef = parentPawn.CurJobDef;
                var curDriver = parentPawn.jobs?.curDriver;
                var checkFlag = false;
                bool restingCheck = false;

                var pawnCaravan = parentPawn.GetCaravan();

                float newBodyFat;
                float newMuscleMass;

                bool doingS = false;
                bool doingB = false;
                bool doingC = false;
                bool UIlimit = false;

                float cardioFactor = 0.3f; //_baseC
                float strengthFactor = 0.1f; //_sedentaryS

                //If factor is Forced
                if (forcedCardio >= 0 && forcedStrength >=0)
                {
                    cardioFactor = forcedCardio;
                    strengthFactor = forcedStrength;
                }
                else if (forceRest)
                {
                    cardioFactor = 0.2f;
                    strengthFactor = 0.0f;
                }
                //Get factors from dedicated Rimbody buildings and items
                else if (jobOverride)
                {
                    cardioFactor = cardioOverride;
                    strengthFactor = strengthOverride;
                    UIlimit = limitOverride;
                    switch (curJobDef.defName)
                    {
                        case "Rimbody_DoStrengthBuilding":
                            {
                                doingS = true;
                            }
                            break;
                        case "Rimbody_DoStrengthLifting":
                            {
                                doingS = true;
                            }
                            break;
                        case "Rimbody_DoBalanceBuilding":
                            {
                                doingB = true;
                            }
                            break;
                        case "Rimbody_DoCardioBuilding":
                            {
                                doingC = true;
                            }
                            break;
                    }
                    strengthFactor = strengthFactor * RimbodySettings.WorkOutGainEfficiency;
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
                        //Special cases: moving
                        if (parentPawn.pather?.MovingNow == true)
                        {
                            switch (parentPawn.jobs.curJob.locomotionUrgency)
                            {
                                case LocomotionUrgency.Sprint:
                                    {
                                        cardioFactor = 2.0f; //_sprintC;
                                        strengthFactor = 0.25f + (carryFactor * RimbodySettings.carryRateMultiplier);
                                        //Jogging
                                        if (curJobDef?.defName == "Rimbody_Jogging")
                                        {
                                            doingC = true;
                                        }
                                        
                                    }
                                    break;
                                case LocomotionUrgency.Jog:
                                    {
                                        cardioFactor = 0.8f; //_joggingC;
                                        strengthFactor = 0.2f + (carryFactor * RimbodySettings.carryRateMultiplier);
                                    }
                                    break;
                                case LocomotionUrgency.Walk:
                                    {
                                        cardioFactor = 0.4f; //_walkingC;
                                        strengthFactor = 0.2f + (carryFactor * RimbodySettings.carryRateMultiplier);
                                    }
                                    break;
                                default:
                                    {
                                        cardioFactor = 0.35f; //_ambleC
                                        strengthFactor = 0.15f + (carryFactor * RimbodySettings.carryRateMultiplier);
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
                        else
                        {
                            //get work factor
                            var jobExtension = curJobDef.GetModExtension<ModExtensionRimbodyJob>();
                            if (jobExtension != null)
                            {
                                if (curJobDef.defName == "Rimbody_DoChunkLifting")
                                {
                                    doingS = true;
                                }
                                cardioFactor = jobExtension.cardio;
                                strengthFactor = jobExtension.strength;
                            }
                            else
                            {
                                var giverExtension = parentPawn?.CurJob?.workGiverDef?.GetModExtension<ModExtensionRimbodyJob>();
                                if (giverExtension != null)
                                {
                                    cardioFactor = giverExtension.cardio;
                                    strengthFactor = giverExtension.strength;
                                }
                            }
                        }
                        //Log.Message($"{parentPawn.Name} doing {curJobDef.defName}. Factors: s:{strengthFactor}, c:{cardioFactor}");
                    }
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
                        musclegainF -= RimbodySettings.maleMusclegain;
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
                        if (isNonSen)
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
                gainF = musclegainF;

                //UI
                if (RimbodySettings.showFleck && parentPawn.IsHashIntervalTick(150))
                {

                    if (doingS)
                    {
                        if (gain == gainMax)
                        {
                            FleckMaker.ThrowMetaIcon(parentPawn.Position, parentPawn.Map, DefOf_Rimbody.Mote_MaxGain);
                        }
                        else if (UIlimit)
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
                        if (UIlimit)
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
                float fatLoss = (BodyFat + 43f) / (40) * cardioFactor; //float fatLoss = (BodyFat + 60f) / (50f) * cardioFactor;
                float fatDelta = (fatGain * fatgainF - fatLoss * fatloseF) * tickRatio * RimbodySettings.rateFactor / 400f;
                newBodyFat = Mathf.Clamp(BodyFat + fatDelta, 0f, 50f);

                //Muscle
                float muscleGain = 0.045f * ((MuscleMass + 75f) / (MuscleMass - 55f) + 25f);
                float muscleLoss = (51.5f / (BodyFat + 50f)) * ((MuscleMass + 50f) / 125f) * Mathf.Pow(((curFood + 0.125f) / 0.125f), -0.5f);
                float muscleDelta = 0f;

                //exhaustion
                float exhaustionDelta = 0f;
                if (RimbodySettings.useExhaustion)
                {
                    exhaustionDelta = -RimbodySettings.CalcEveryTick * (0.7f - (7f * (BodyFat) / 100f * (BodyFat - 50f) / 100f * (BodyFat + 100f) / 100f)) * ((MuscleMass / 50) + 1) / 1000f;
                }
                

                if (parentPawn.needs.rest != null)
                {
                    //Grow on sleep
                    if (forceRest || (pawnCaravan != null && (!pawnCaravan.pather.MovingNow || parentPawn.InCaravanBed() || parentPawn.CarriedByCaravan())) || (pawnCaravan == null && parentPawn.jobs?.curDriver?.asleep == true))
                    {
                        restingCheck = true;
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
                        exhaustionDelta = 8f * exhaustionDelta;
                    }
                    //Awake
                    else
                    {
                        //Store gain
                        gain = Mathf.Clamp(gain + (strengthFactor * musclegainF * muscleGain * tickRatio), 0f, gainMax);
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
                        muscleDelta += recoveryFactor * tickRatio * (RimbodySettings.rateFactor / 400f);
                    }
                    else if (gain > 0f)
                    {
                        muscleDelta += (RimbodySettings.rateFactor / 400f) * gain;
                        gain = 0f;
                    }
                    exhaustionDelta = 4f * exhaustionDelta;
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

                //Manage fatigue and exhaustion
                if(durationOverride>0 && fatigueOverride != null)
                {
                    AddPartFatigue(fatigueOverride, (float)RimbodySettings.CalcEveryTick/durationOverride);
                }
                else
                {
                    RestorePartFatigue(restingCheck?2f:1f);
                }

                if (RimbodySettings.useExhaustion)
                {
                    if (doingS)
                    {
                        exhaustionDelta = RimbodySettings.CalcEveryTick / (25f * (0.5f + (5f * (MuscleMass / 100f)) + (4f * (BodyFat / 100f) * (MuscleMass / 100f)) + (2f * (BodyFat / 100f))));
                        //Log.Message($"{parentPawn.Name} Strength Training. fatigueDelta: {fatigueDelta}");
                    }
                    else if (doingB)
                    {
                        exhaustionDelta = RimbodySettings.CalcEveryTick / (25f * (1f + (70f * (BodyFat) / 100f * (BodyFat - 50f) / 100f * (BodyFat - 100f) / 100f)) * (1f - ((5f * (MuscleMass / 100f) * (MuscleMass - 25f - RimbodySettings.muscleThresholdHulk)) / 100f)));
                        //Log.Message($"{parentPawn.Name} Balance Training. fatigueDelta: {fatigueDelta}");
                    }
                    else if (doingC)
                    {
                        exhaustionDelta = RimbodySettings.CalcEveryTick / (25f * (1f + (70f * (BodyFat) / 100f * (BodyFat - 50f) / 100f * (BodyFat - 100f) / 100f)) * (1f - ((5f * (MuscleMass / 100f) * (MuscleMass - 25f - RimbodySettings.muscleThresholdHulk)) / 100f)));
                        //Log.Message($"{parentPawn.Name} Cardio Training. fatigueDelta: {fatigueDelta}");
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
            if (this.memory.Count >= 3)
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
                    partFatigue[i] = Math.Max(0f, Math.Min((strengthParts[i]* multiplier) + partFatigue[i], RimbodySettings.MaxFatiguePerPart));
                }
            }
        }
        public void RestorePartFatigue(float multiplier)
        {
            if (partFatigue?.Count == RimbodySettings.PartCount)
            {
                for (int i = 0; i < RimbodySettings.PartCount; i++)
                {
                    partFatigue[i] = Math.Max(0f, partFatigue[i] - (multiplier * RimbodySettings.CalcEveryTick/5000f));
                }
            }
        }

        public float GetStrengthPartScore(List<float> strengthParts, float strength)
        {
            if(partFatigue == null || strengthParts.Count != RimbodySettings.PartCount) return 0f;
            float fatigueFactor = 0f;
            float total = 0;
            float spread = 0f;
            float peak = 0f;
            for (int i = 0; i < RimbodySettings.PartCount; i++)
            {
                if (strengthParts[i] > 0)
                {
                    fatigueFactor += strengthParts[i] * (10f - partFatigue[i]) / 10f;
                    spread = spread + Math.Min(1f, strengthParts[i]);
                    total = total + strengthParts[i];
                    peak = Math.Max(peak, strengthParts[i]);
                }
            }
            fatigueFactor = fatigueFactor / total;
            float fi = (total + ((0.1f * ((float)RimbodySettings.PartCount - spread)) * peak))/4f;
            return strength * fatigueFactor * fi;
        }

        public float GetScore(RimbodyTargetCategory category, WorkOut workout)
        {
            switch (category)
            {
                case RimbodyTargetCategory.Strength:
                    if(workout.strengthParts == null)
                    {
                        return 0f;
                    }
                    return GetStrengthPartScore(workout.strengthParts, workout.strength);
                case RimbodyTargetCategory.Cardio:
                    return memory.Contains("cardio|" + workout.name) ? 0.9f*workout.cardio : workout.cardio;
                case RimbodyTargetCategory.Balance:
                    return memory.Contains("balance|" + workout.name) ? 0.9f : 1f;
            }
            return 0;
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
            Scribe_Values.Look(ref limitOverride, "Physique_limitOverride", false);
            Scribe_Values.Look(ref cardioOverride, "Physique_cardioOverride", 0f);
            Scribe_Values.Look(ref strengthOverride, "Physique_strengthOverride", 0f);
            Scribe_Values.Look(ref durationOverride, "Physique_durationOverride", 0);
            Scribe_Collections.Look(ref fatigueOverride, "Physique_fatigueOverride", LookMode.Value);
            if (fatigueOverride == null || fatigueOverride.Count != RimbodySettings.PartCount)
            {
                fatigueOverride = Enumerable.Repeat(0f, RimbodySettings.PartCount).ToList();
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
