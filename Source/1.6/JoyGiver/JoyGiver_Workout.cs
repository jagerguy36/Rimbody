using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JoyGiver_Workout : JoyGiver
    {
        private static readonly List<Thing> tmpCandidates = [];
        private static readonly Dictionary<int, float> workoutCache = [];
        private static readonly (RimbodyWorkoutCategory type, Func<CompPhysique, float> getPriority, Func<Pawn, List<Thing>, Dictionary<int, float>, Job> tryGiveJob)[] WorkoutGivers =
        [
            (RimbodyWorkoutCategory.Strength, JobGiver_DoStrengthBuilding.GetActualPriority, JobGiver_DoStrengthBuilding.TryGiveJobActual),
            (RimbodyWorkoutCategory.Balance, JobGiver_DoBalanceBuilding.GetActualPriority, JobGiver_DoBalanceBuilding.TryGiveJobActual),
            (RimbodyWorkoutCategory.Cardio, JobGiver_DoCardioBuilding.GetActualPriority, JobGiver_DoCardioBuilding.TryGiveJobActual)
        ];
        public override float GetChance(Pawn pawn)
        {
            if (!RimbodySettings.workoutDuringRecTime)
            {
                return 0f;
            }
            return base.GetChance(pawn);
        }

        public override bool CanBeGivenTo(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (compPhysique == null)
                return false;
            // Exhaustion not implemented yet
            // if (RimbodySettings.useExhaustion && compPhysique.resting)
            //     return false;
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
                return false;
            if (pawn.ageTracker?.CurLifeStage?.developmentalStage != DevelopmentalStage.Adult)
                return false;
            if (Rimbody_Utility.TooTired(pawn))
                return false;
            if (!pawn.IsColonist && !pawn.IsPrisonerOfColony)
                return false;
            if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
                return false;
            return base.CanBeGivenTo(pawn);
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            var topGivers = new (RimbodyWorkoutCategory type, float priority, Func<Pawn, List<Thing>, Dictionary<int, float>, Job> tryGiveJob)[3];
            int count = 0;

            foreach (var (type, getPriority, tryGiveJob) in WorkoutGivers)
            {
                float priority = getPriority(compPhysique);
                if (priority <= 0f) continue;

                // Insert into top 3 list
                for (int i = 0; i <= count; i++)
                {
                    if (i == count || priority > topGivers[i].priority)
                    {
                        if (count < 3) count++;
                        for (int j = count - 1; j > i; j--)
                            topGivers[j] = topGivers[j - 1];
                        topGivers[i] = (type, priority, tryGiveJob);
                        break;
                    }
                }
            }

            foreach (var (type, priority, tryGiveJob) in topGivers)
            {
                if (tryGiveJob == null || priority <= 0f) continue;
                Job job = tryGiveJob(pawn, tmpCandidates, workoutCache);
                tmpCandidates.Clear();
                workoutCache.Clear();
                if (job != null)
                {
                    compPhysique.AssignedTick = 2000;
                    return job;
                }
            }
            return null;
        }
    }
}
