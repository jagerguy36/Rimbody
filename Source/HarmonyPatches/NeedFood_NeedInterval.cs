
using HarmonyLib;
using RimWorld;
using System;
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

        private static GeneDef NonSenescent = ModsConfig.BiotechActive ? DefDatabase<GeneDef>.GetNamed("DiseaseFree", true) : null;
        public static void Postfix(Need_Food __instance)
        {
            var pawnField = typeof(Need).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
            var pawn = (Pawn)pawnField.GetValue(__instance);


            if (pawn != null && (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony))
            {

                var compPhysique = pawn.TryGetComp<CompPhysique>();
                if (compPhysique == null || compPhysique.BodyFat <= -1 || compPhysique.MuscleMass <= -1){
                    return;
                }

                var frozenProperty = typeof(Need).GetProperty("IsFrozen", BindingFlags.NonPublic | BindingFlags.Instance);
                var isFoodNeedFrozen = (bool)frozenProperty.GetValue(__instance);
                if (isFoodNeedFrozen)
                {
                    return;
                }
                var stringified = string.Join(", ", compPhysique.memory);
                Log.Message($"{pawn.Name}'s memory: {stringified}");

                var curFood = pawn.needs.food.CurLevel;
                var curJob = pawn.CurJobDef;
                var checkFlag = false;
                float newBodyFat;
                float newMuscleMass;

                //get work factor
                var jobExtension = pawn.CurJobDef.GetModExtension<ModExtentionRimbodyJob>();
                float cardioFactor = 1.0f;
                float strengthFactor = 0.2f;
                //Special cases: moving
                if (pawn.pather.MovingNow)
                {
                    switch (pawn.jobs.curJob.locomotionUrgency)
                    {
                        case LocomotionUrgency.Sprint:
                            {
                                cardioFactor = 1.6f;
                                strengthFactor = 0.4f;
                            }
                            break;
                        case LocomotionUrgency.Jog:
                            {
                                cardioFactor = 1.3f;
                                strengthFactor = 0.35f;
                            }
                            break;
                        default:
                            {
                                cardioFactor = 1.1f;
                                strengthFactor = 0.3f;
                            }
                            break;

                    }
                }
                //Get factors from dedicated Rimbody buildings
                else if (curJob.defName == "Rimbody_DoStrengthBuilding" || curJob.defName == "Rimbody_DoBalanceBuilding" || curJob.defName == "Rimbody_DoCardioBuilding")
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


                if (RimbodySettings.showFleck && strengthFactor >= 2)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, DefOf_Rimbody.Mote_Gain);
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
                            strengthFactor -= 1.5f;
                            break;
                        case RestCategory.Exhausted:
                            strengthFactor -= 2f;
                            break;
                        default:
                            break;
                    }
                }


                //Age Factors in
                var fatgainF = compPhysique.FatGainFactor;
                var fatloseF = compPhysique.FatLoseFactor;
                var musclegainF = compPhysique.MuscleGainFactor;
                var muscleloseF = compPhysique.MuscleLoseFactor;

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
                            fatgainF += (Math.Min(pawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint) - 25) / 1000;
                            fatloseF -= (Math.Min(pawn.ageTracker.AgeBiologicalYears, RimbodySettings.nonSenescentpoint) - 25) / 1000;
                        }
                        else
                        {
                            fatgainF += (pawn.ageTracker.AgeBiologicalYears - 25) / 1000;
                            fatloseF -= (pawn.ageTracker.AgeBiologicalYears - 25) / 1000;
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
                    if (curJob.defName == "LayDown" || curJob.defName == "LayDownResting" || curJob.defName == "LayDownAwake")
                    {
                        var swol = 2f;
                        if (curJob.defName == "LayDownAwake")
                        {
                            swol = 1.5f;
                        }
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
                        compPhysique.gain = Mathf.Clamp(compPhysique.gain + (strengthFactor * musclegainF * muscleGain), 0f, ((3f * compPhysique.MuscleMass) + 75f));
                    }
                }
                //Sleepless pawns.
                else
                {
                    muscleDelta += (RimbodySettings.rateFactor / 400f) * (strengthFactor * musclegainF * muscleGain);
                }

                muscleDelta -= (RimbodySettings.rateFactor / 400f) * muscleloseF * muscleLoss;
                newMuscleMass = Mathf.Clamp(compPhysique.MuscleMass + muscleDelta, 0f, 50f);

                if (fatDelta > 0f)
                {
                    if (compPhysique.BodyFat < RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod && newBodyFat >= RimbodySettings.fatThresholdThin + RimbodySettings.gracePeriod)
                    {
                        checkFlag = true;
                    }
                    else if (compPhysique.BodyFat < RimbodySettings.fatThresholdFat + RimbodySettings.gracePeriod && newBodyFat >= RimbodySettings.fatThresholdFat + RimbodySettings.gracePeriod)
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
                    else if (compPhysique.BodyFat > RimbodySettings.fatThresholdFat - RimbodySettings.gracePeriod && newBodyFat <= RimbodySettings.fatThresholdFat - RimbodySettings.gracePeriod)
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


                //Apply New Values
                compPhysique.BodyFat = newBodyFat;
                compPhysique.MuscleMass = newMuscleMass;

                //BodyChange
                if (checkFlag)
                {
                    pawn.story.bodyType = compPhysique.GetValidBody(pawn);
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                
            }
        }
    }
}
