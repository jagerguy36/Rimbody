using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JoyGiver_Workout : JoyGiver
    {
        private static List<Thing> tmpCandidates = [];
        private static Dictionary<int, float> workoutCache = new Dictionary<int, float>();
        private static readonly (RimbodyWorkoutCategory type, Func<CompPhysique, float> getPriority, Func<Pawn, List<Thing>, Dictionary<int, float>, Job> tryGiveJob)[] WorkoutGivers =
        {
            (RimbodyWorkoutCategory.Strength, JobGiver_DoStrengthBuilding.GetActualPriority, JobGiver_DoStrengthBuilding.TryGiveJobActual),
            (RimbodyWorkoutCategory.Balance, JobGiver_DoBalanceBuilding.GetActualPriority, JobGiver_DoBalanceBuilding.TryGiveJobActual),
            (RimbodyWorkoutCategory.Cardio, JobGiver_DoCardioBuilding.GetActualPriority, JobGiver_DoCardioBuilding.TryGiveJobActual)
        };
        public override float GetChance(Pawn pawn)
        {
            if (!RimbodySettings.workoutDuringRecTime)
            {
                return 0f;
            }
            return base.GetChance(pawn);
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (pawn != null && pawn.ageTracker?.CurLifeStage?.developmentalStage == DevelopmentalStage.Adult)
            {
                if (Rimbody_Utility.TooTired(pawn)) //Too tired
                {
                    return null;
                }
                if (pawn.IsColonist || pawn.IsPrisonerOfColony)
                {
                    if (compPhysique == null) return null;
                    if (RimbodySettings.useExhaustion && compPhysique.resting) return null;
                    if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick) return null;
                    if (HealthAIUtility.ShouldSeekMedicalRest(pawn)) return null;
                    bool noStrength = compPhysique.gain >= compPhysique.gainMax * RimbodySettings.gainMaxGracePeriod;
                    var topGivers = new (RimbodyWorkoutCategory type, float priority, Func<Pawn, List<Thing>, Dictionary<int, float>, Job> tryGiveJob)[3];
                    int count = 0;

                    foreach (var giver in WorkoutGivers)
                    {
                        float priority = (noStrength && giver.type == RimbodyWorkoutCategory.Strength) ? 0f : giver.getPriority(compPhysique);
                        if (priority <= 0f)
                            continue;

                        // Insert into top 3 list
                        for (int i = 0; i <= count; i++)
                        {
                            if (i == count || priority > topGivers[i].priority)
                            {
                                if (count < 3) count++;
                                for (int j = count - 1; j > i; j--)
                                    topGivers[j] = topGivers[j - 1];
                                topGivers[i] = (giver.type, priority, giver.tryGiveJob);
                                break;
                            }
                        }
                    }

                    foreach (var giver in topGivers)
                    {
                        if (giver.tryGiveJob == null)
                            continue;
                        if (giver.priority <= 0f) return null;

                        tmpCandidates.Clear();
                        workoutCache.Clear();

                        Job job = giver.tryGiveJob(pawn, tmpCandidates, workoutCache);
                        if (job != null)
                        {
                            compPhysique.AssignedTick = 2000;
                            return job;
                        }
                    }

                    return null;
                }
            }
            return null;
        }
    }
}
