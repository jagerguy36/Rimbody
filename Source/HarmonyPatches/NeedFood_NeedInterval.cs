
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    [HarmonyPatch(typeof(Need_Food), "NeedInterval")]
    public class NeedFood_NeedInterval
    {

        private static readonly GeneDef NonSenescent = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("DiseaseFree", true) : null;
        public static readonly HashSet<string> RimbodyJobs = new HashSet<string>
        {
            "Rimbody_DoStrengthBuilding",
            "Rimbody_DoBalanceBuilding",
            "Rimbody_DoCardioBuilding",
        };

        //private static readonly float _sprintC = 2.0f;
        //private static readonly float _hardworkC = 1.0f; construction, smoothing, mining, replanting, extractTrees
        //private static readonly float _workC = 0.85f; harvesting, repairing, empty wasters, clean pollutions
        //private static readonly float _joggingC = 0.8f;
        //private static readonly float _walkingC = 0.4f; light physical works
        //private static readonly float _ambleC = 0.35f;
        //private static readonly float _baseC = 0.3f; sedentary works, standing
        //private static readonly float _sleepC = 0.2f; laying down

        //private static readonly float _workoutS = 2.0f;
        //private static readonly float _hardworkS = 1.2f;
        //private static readonly float _workS = 0.8f;
        //private static readonly float _sprintS = 0.25f;
        //private static readonly float _movingS = 0.2f; light physical works
        //private static readonly float _ambleS = 0.15f;
        //private static readonly float _sedentaryS = 0.1f; sedentary, standing
        //private static readonly float _lyingS = 0.0f; laying down


        public static void Postfix(Need_Food __instance)
        {
            var pawnField = typeof(Need).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
            var pawn = (Pawn)pawnField.GetValue(__instance);
            var compPhysique = pawn?.TryGetComp<CompPhysique>();
            var curJob = pawn.CurJobDef;

            if (pawn?.needs != null && (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || pawn.IsColonist && pawn.GetCaravan() != null || (pawn.IsColonist && curJob!=null)))
            {
                if (compPhysique == null || compPhysique.BodyFat <= -1f || compPhysique.MuscleMass <= -1f) {
                    return;
                }

                var frozenProperty = typeof(Need).GetProperty("IsFrozen", BindingFlags.NonPublic | BindingFlags.Instance);
                var isFoodNeedFrozen = (bool)frozenProperty.GetValue(__instance);
                if (isFoodNeedFrozen)
                {
                    return;
                }

                var curFood = Mathf.Clamp(pawn.needs.food.CurLevel, 0f, 1f);
                var curDriver = pawn.jobs?.curDriver;
                var curToil = curDriver?.CurToilString;
                var checkFlag = false;

                var pawnCaravan = pawn.GetCaravan();

                float newBodyFat;
                float newMuscleMass;
                //Factors based on jobs
                float cardioFactor = 0.3f; //_baseC
                float strengthFactor = 0.1f; //_sedentaryS
                //Factors for Caravan
                if (pawnCaravan!=null)
                {
                    //Resting
                    if (!pawnCaravan.pather.MovingNow || pawn.InCaravanBed() || pawn.CarriedByCaravan())
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
                else if (curJob != null && curDriver != null)
                {
                    //get work factor
                    var jobExtension = curJob.GetModExtension<ModExtentionRimbodyJob>();
                    //Special cases: moving
                    if (pawn.pather?.MovingNow == true)
                    {
                        switch (pawn.jobs.curJob.locomotionUrgency)
                        {
                            case LocomotionUrgency.Sprint:
                                {
                                    cardioFactor = 2.0f; //_sprintC;
                                    strengthFactor = 0.25f;
                                }
                                break;
                            case LocomotionUrgency.Jog:
                                {
                                    cardioFactor = 0.8f; //_joggingC;
                                    strengthFactor = 0.2f;
                                }
                                break;
                            case LocomotionUrgency.Walk:
                                {
                                    cardioFactor = 0.4f; //_walkingC;
                                    strengthFactor = 0.2f;
                                }
                                break;
                            default:
                                {
                                    cardioFactor = 0.35f; //_ambleC
                                    strengthFactor = 0.15f;
                                }
                                break;

                        }
                    }
                    //Special cases: Lying down
                    else if (curToil == "LayDown")
                    {
                        cardioFactor = 0.2f;
                        strengthFactor = 0.0f;
                    }
                    //Get factors from dedicated Rimbody buildings
                    else if (RimbodyJobs.Contains(curJob.defName))
                    {
                        var curjobTarget = pawn.CurJob.targetA;
                        var buildingExtention = curjobTarget.Thing.def.GetModExtension<ModExtentionRimbodyBuilding>();
                        if (buildingExtention != null)
                        {
                            cardioFactor = buildingExtention.cardio;
                            strengthFactor = buildingExtention.strength;
                        }

                    }
                    else if (jobExtension != null)
                    {
                        cardioFactor = jobExtension.cardio;
                        strengthFactor = jobExtension.strength;
                    }
                }

                if (compPhysique.forceRest)
                {
                    cardioFactor = 0.2f;
                    strengthFactor = 0.0f;
                }

                //UI
                if (RimbodySettings.showFleck && strengthFactor >= 2f)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, DefOf_Rimbody.Mote_Gain);
                }
                if (RimbodySettings.showFleck && cardioFactor >= 2f)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, DefOf_Rimbody.Mote_Cardio);
                }
                //Tiredness reduces gain
                if (pawn.needs.rest != null)
                {
                    switch (pawn.needs.rest.CurCategory)
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
                var fatgainF = compPhysique.FatGainFactor;
                var fatloseF = compPhysique.FatLoseFactor;
                var musclegainF = compPhysique.MuscleGainFactor;
                var muscleloseF = compPhysique.MuscleLoseFactor;
                float fatThreshholdE = 0.0f;

                //Gender
                if (RimbodySettings.genderDifference == true)
                {
                    if(pawn.gender == Gender.Male)
                    {
                        musclegainF += RimbodySettings.maleMusclegain;
                    }
                    else
                    {
                        fatThreshholdE = RimbodySettings.femaleFatThreshold;
                    }
                }
                //Age Factors in
                switch (pawn.ageTracker?.CurLifeStage?.developmentalStage)
                {
                    case DevelopmentalStage.None:
                        break;

                    case DevelopmentalStage.Newborn:
                        fatgainF -= 0.15f;
                        fatloseF += 0.25f;
                        break;

                    case DevelopmentalStage.Baby:
                        fatgainF -= 0.15f;
                        fatloseF += 0.25f;
                        break;

                    case DevelopmentalStage.Child:
                        fatgainF -= 0.12f;
                        fatloseF += 0.12f;
                        break;

                    case DevelopmentalStage.Adult:
                        if (NonSenescent !=null && pawn.genes.HasActiveGene(NonSenescent))
                        {
                            fatgainF += (float)(Math.Min(pawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint) - 25) / 1000f;
                            fatloseF -= (float)(Math.Min(pawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint) - 25) / 1000f;
                        }
                        else
                        {
                            fatgainF += (float)(Math.Min(pawn.ageTracker.AgeBiologicalYears, 125) - 25) / 1000f;
                            fatloseF -= (float)(Math.Min(pawn.ageTracker.AgeBiologicalYears, 125) - 25) / 1000f;
                        }
                        break;
                }
                //Fat
                float fatGain = Mathf.Pow(curFood, 0.5f);
                float fatLoss = (compPhysique.BodyFat + 100f) / (190f) * cardioFactor;
                float fatDelta = RimbodySettings.rateFactor/400f*(fatGain * fatgainF - fatLoss * fatloseF);
                newBodyFat = Mathf.Clamp(compPhysique.BodyFat + fatDelta, 0f, 50f);

                //Muscle
                float muscleGain = 0.25f * ((compPhysique.MuscleMass + 75f) / (compPhysique.MuscleMass - 75f) + 5f);
                float muscleLoss = 0.75f * ((compPhysique.MuscleMass + 100f)/ 190f) * Mathf.Pow(((curFood + 0.125f) / 0.125f), -0.5f);
                float muscleDelta = 0f;

                if (pawn.needs.rest != null)
                {
                    //Grow on sleep
                    if (compPhysique.forceRest || (pawnCaravan!=null && (!pawnCaravan.pather.MovingNow || pawn.InCaravanBed() || pawn.CarriedByCaravan())) || (pawnCaravan == null && pawn.jobs?.curDriver?.asleep == true))
                    {
                        var swol = 2f;
                        var rrm = pawn.GetStatValue(StatDefOf.RestRateMultiplier);
                        var bre = pawn.CurrentBed()?.GetStatValue(StatDefOf.BedRestEffectiveness) ?? 0.8f;
                        var recoveryFactor = swol * rrm * bre;

                        if (compPhysique.gain - recoveryFactor > 0f)
                        {
                            compPhysique.gain -= recoveryFactor;
                            muscleDelta += (RimbodySettings.rateFactor / 400f) * recoveryFactor;
                        }
                        else if (compPhysique.gain > 0f)
                        {
                            muscleDelta += (RimbodySettings.rateFactor / 400f) * compPhysique.gain;
                            compPhysique.gain = 0f;
                        }
                    }
                    //Store gain
                    else
                    {
                        compPhysique.gain = Mathf.Clamp(compPhysique.gain + (strengthFactor * musclegainF * muscleGain), 0f, (2f * compPhysique.MuscleMass * musclegainF)+100f);
                    }
                }
                //Sleepless pawns.
                else
                {
                    muscleDelta += (RimbodySettings.rateFactor / 400f) * (strengthFactor * musclegainF * muscleGain);
                }
                muscleDelta -= (RimbodySettings.rateFactor / 400f) * muscleloseF * muscleLoss;
                newMuscleMass = Mathf.Clamp(compPhysique.MuscleMass + muscleDelta, 0f, 50f);
                //BodyChange Check
                if (fatDelta > 0f)
                {
                    if (compPhysique.BodyFat < RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod && newBodyFat >= RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod)
                    {                       
                        checkFlag = true;
                    }
                    else if (compPhysique.BodyFat < RimbodySettings.fatThresholdFat + RimbodySettings.gracePeriod && newBodyFat >= RimbodySettings.fatThresholdFat + fatThreshholdE + RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                }
                else if (fatDelta < 0f)
                {
                    if (compPhysique.BodyFat > RimbodySettings.fatThresholdThin - RimbodySettings.gracePeriod && newBodyFat <= RimbodySettings.fatThresholdThin - RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                    else if (compPhysique.BodyFat > RimbodySettings.fatThresholdFat - RimbodySettings.gracePeriod && newBodyFat <= RimbodySettings.fatThresholdFat + fatThreshholdE - RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                }

                if (muscleDelta > 0f)
                {
                    if (compPhysique.MuscleMass < RimbodySettings.muscleThresholdThin + RimbodySettings.gracePeriod && newMuscleMass >= RimbodySettings.muscleThresholdThin + RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                    else if (compPhysique.MuscleMass < RimbodySettings.muscleThresholdHulk + RimbodySettings.gracePeriod && newMuscleMass >= RimbodySettings.muscleThresholdHulk + RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                }
                else if (muscleDelta < 0f)
                {
                    if (compPhysique.MuscleMass > RimbodySettings.muscleThresholdThin - RimbodySettings.gracePeriod && newMuscleMass <= RimbodySettings.muscleThresholdThin - RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                    else if (compPhysique.MuscleMass > RimbodySettings.muscleThresholdHulk - RimbodySettings.gracePeriod && newMuscleMass <= RimbodySettings.muscleThresholdHulk - RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                }
                Log.Message($"{pawn.Name} got past the null reference check. Adjusting with strenght: {strengthFactor}, cardio: {cardioFactor}");


                //Apply New Values
                compPhysique.BodyFat = newBodyFat;
                compPhysique.MuscleMass = newMuscleMass;

                //BodyChange
                if (checkFlag == true)
                {
                    pawn.story.bodyType = compPhysique.GetValidBody(pawn);
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                
            }
        }
    }
}
