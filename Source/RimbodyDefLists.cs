using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Verse;
using static UnityEngine.Scripting.GarbageCollector;
using static Verse.AI.ThingCountTracker;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public class RimbodyDefLists
    {
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> StrengthTarget = new();
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> CardioTarget = new();
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> BalanceTarget = new();
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> WorkoutBuilding = new();
        public static Dictionary<JobDef, ModExtensionRimbodyJob> StrengthNonTargetJob = new(); //For JobGiver
        public static Dictionary<JobDef, ModExtensionRimbodyJob> CardioNonTargetJob = new(); //For JobGiver
        public static Dictionary<JobDef, ModExtensionRimbodyJob> BalanceNonTargetJob = new(); //For JobGiver
        public static HashSet<string> StrengthJob = new HashSet<string>{ "Rimbody_DoStrengthLifting", "Rimbody_DoStrengthBuilding" }; //For Flags
        public static HashSet<string> CardioJob = new HashSet<string> { "Rimbody_DoCardioBuilding" }; //For Flags
        public static HashSet<string> BalanceJob = new HashSet<string> { "Rimbody_DoBalanceLifting", "Rimbody_DoBalanceBuilding" }; //For Flags
        public static List<float> jogging_parts;
        public static List<float> jogging_parts_jogger;
        public static float strengthHighscore = 0;
        public static float cardioHighscore = 0;
        public static float balanceHighscore = 0;

        public static Dictionary<string, ModExtensionRimbodyJob> JobExtensionCache = new();
        public static Dictionary<string, ModExtensionRimbodyJob> GiverExtensionCache = new();

        static RimbodyDefLists() // Static constructor
        {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                var buildingExtension = thingDef.GetModExtension<ModExtensionRimbodyTarget>();
                if (buildingExtension != null)
                {
                    AddTarget(thingDef, buildingExtension);
                }
            }

            foreach (var jobDef in DefDatabase<JobDef>.AllDefs)
            {
                var jobExtension = jobDef.GetModExtension<ModExtensionRimbodyJob>();
                if (jobExtension?.Category !=null && jobExtension?.Category != RimbodyWorkoutCategory.Job)
                {
                    AddJob(jobDef, jobExtension);
                }
                if(jobExtension != null)
                {
                    JobExtensionCache[jobDef.defName] = jobExtension;
                }

            }

            foreach (var giverDef in DefDatabase<WorkGiverDef>.AllDefs)
            {
                var giverExtension = giverDef.GetModExtension<ModExtensionRimbodyJob>();
                if(giverExtension != null)
                {
                    GiverExtensionCache[giverDef.defName] = giverExtension;
                }
            }
        }

        private static void AddTarget(ThingDef targetDef, ModExtensionRimbodyTarget targetExtension)
        {
            foreach (var workout in targetExtension.workouts)
            {
                switch (workout.Category)
                {
                    case RimbodyWorkoutCategory.Strength:
                        StrengthTarget[targetDef] = targetExtension;
                        break;
                    case RimbodyWorkoutCategory.Balance:
                        BalanceTarget[targetDef] = targetExtension;
                        break;
                    case RimbodyWorkoutCategory.Cardio:
                        CardioTarget[targetDef] = targetExtension;
                        break;
                }
            }

            if (targetExtension.Type == RimbodyTargetType.Building)
            {
                WorkoutBuilding[targetDef] = targetExtension;
            }
        }

        private static void AddJob(JobDef jobDef, ModExtensionRimbodyJob jobExtension)
        {
            switch (jobExtension.Category)
            {
                case RimbodyWorkoutCategory.Strength:
                    if (jobExtension.strengthParts != null)
                    {
                        var os = GetOptimalStrengthPartScore(jobExtension.strengthParts, jobExtension.strength);
                        strengthHighscore = Math.Max(strengthHighscore, os);
                        StrengthNonTargetJob[jobDef] = jobExtension;
                        StrengthJob.Add(jobDef.defName);
                    }
                    break;
                case RimbodyWorkoutCategory.Balance:
                    if (jobExtension.strengthParts != null)
                    {
                        var os = GetOptimalStrengthPartScore(jobExtension.strengthParts, jobExtension.strength);
                        balanceHighscore = Math.Max(balanceHighscore, os);
                        BalanceNonTargetJob[jobDef] = jobExtension;
                        BalanceJob.Add(jobDef.defName);
                    }
                    break;
                case RimbodyWorkoutCategory.Cardio:
                    if (jobExtension.strengthParts != null)
                    {
                        if(jobDef.defName == "Rimbody_Jogging")
                        {
                            jogging_parts = jobExtension.strengthParts;
                            jogging_parts_jogger = jobExtension.strengthParts.Select(x => x * 0.5f).ToList();
                        }
                        cardioHighscore = Math.Max(cardioHighscore, GetOptimalCardioJobScore(jobExtension.strengthParts, jobExtension.cardio));
                        CardioNonTargetJob[jobDef] = jobExtension;
                        CardioJob.Add(jobDef.defName);
                    }   
                    break;
            }
        }

        public static float GetOptimalStrengthPartScore(List<float> strengthParts, float strength)
        {
            if (strengthParts.Count != RimbodySettings.PartCount) return 0f;
            float total = 0;
            float spread = 0f;
            float peak = 0f;
            for (int i = 0; i < RimbodySettings.PartCount; i++)
            {
                if (strengthParts[i] > 0)
                {
                    spread = spread + Math.Min(1f, strengthParts[i]);
                    total = total + strengthParts[i];
                    peak = Math.Max(peak, strengthParts[i]);
                }
            }
            float fi = (total + ((0.1f * ((float)RimbodySettings.PartCount - spread)) * peak)) / 4f;
            return strength * fi;
        }

        public static float GetOptimalCardioJobScore(List<float> strengthParts, float cardio)
        {
            if (strengthParts.Count != RimbodySettings.PartCount) return 0f;
            float fatigueDet = 0f;
            for (int i = 0; i < RimbodySettings.PartCount; i++)
            {
                if (strengthParts[i] > 0)
                {
                    fatigueDet += strengthParts[i];
                }
            }
            fatigueDet = 90f - fatigueDet;
            return cardio * fatigueDet;
        }
    }
}


