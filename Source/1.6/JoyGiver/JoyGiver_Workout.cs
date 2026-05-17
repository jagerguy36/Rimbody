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
        private static readonly (RimbodyWorkoutCategory type, float priority, Func<Pawn, List<Thing>, Dictionary<int, float>, Job> tryGiveJob)[] topGivers = new (RimbodyWorkoutCategory, float, Func<Pawn, List<Thing>, Dictionary<int, float>, Job>)[3];
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
            topGivers[0].priority = 0f;
            topGivers[1].priority = 0f;
            topGivers[2].priority = 0f;
            int count = 0;

            for (int i = 0; i < 3; i++)
            {
                var giver = WorkoutGivers[i];
                float priority = giver.getPriority(compPhysique);
                if (priority <= 0f) continue;

                // Insert into top 3 list
                for (int j = 0; j <= count; j++)
                {
                    if (j == count || priority > topGivers[j].priority)
                    {
                        if (count < 3) count++;
                        for (int k = count - 1; k > j; k--)
                            topGivers[k] = topGivers[k - 1];

                        topGivers[j] = (giver.type, priority, giver.tryGiveJob);
                        break;
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                var current = topGivers[i];
                if (current.priority <= 0f || current.tryGiveJob == null) continue;
                Job job = current.tryGiveJob(pawn, tmpCandidates, workoutCache);
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
