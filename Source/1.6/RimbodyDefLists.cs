using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public class RimbodyDefLists
    {
        public static List<Thing> StrengthTarget = new();
        public static List<Thing> CardioTarget = new();
        public static List<Thing> BalanceTarget = new();
        public static Dictionary<int, ModExtensionRimbodyTarget> ThingModExDB = new();
        public static Dictionary<int, ModExtensionRimbodyJob> JobModExDB = new();
        public static Dictionary<int, ModExtensionRimbodyJob> GiverModExDB = new();
        public static HashSet<int> WorkoutBuildingHash = new();

        public static HashSet<int> StrengthJob = new();
        public static HashSet<int> CardioJob = new();
        public static HashSet<int> BalanceJob = new();
        public static HashSet<string> StrengthJob = new HashSet<string>{ "Rimbody_DoStrengthLifting", "Rimbody_DoStrengthBuilding" }; //For Flags
        public static HashSet<string> CardioJob = new HashSet<string> { "Rimbody_DoCardioBuilding" }; //For Flags
        public static HashSet<string> BalanceJob = new HashSet<string> { "Rimbody_DoBalanceLifting", "Rimbody_DoBalanceBuilding" }; //For Flags
        public static List<float> jogging_parts;
        public static List<float> jogging_parts_jogger;
        public static float strengthHighscore = 0;
        public static float cardioHighscore = 0;
        public static float balanceHighscore = 0;

        public static HashSet<int> RelevantGenes = new();
        // public static HashSet<string> RelevantGenes = new HashSet<string>{"Body_Fat", "Body_Thin", "Body_Hulk", "Body_Standard", "DiseaseFree"};

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

            RegisterGeneFactors();

            RegisterJobs();
        }

        private static void AddTarget(ThingDef targetDef, ModExtensionRimbodyTarget targetExtension)
        {
            foreach (var workout in targetExtension.workouts)
            {
                ThingModExDB[targetDef.shortHash] = targetExtension;
                switch (workout.Category)
                {
                    case RimbodyWorkoutCategory.Strength:
                        StrengthTarget.Add(targetDef);
                        break;
                    case RimbodyWorkoutCategory.Balance:
                        BalanceTarget.Add(targetDef);
                        break;
                    case RimbodyWorkoutCategory.Cardio:
                        CardioTarget.Add(targetDef);
                        break;
                }
            }

            if (targetExtension.Type == RimbodyTargetType.Building)
            {
                WorkoutBuildingHash.Add(targetDef.int);
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
                        JobModExDB[jobDef.shortHash] = jobExtension;
                        StrengthJob.Add(jobDef.shortHash);
                    }
                    break;
                case RimbodyWorkoutCategory.Balance:
                    if (jobExtension.strengthParts != null)
                    {
                        var os = GetOptimalStrengthPartScore(jobExtension.strengthParts, jobExtension.strength);
                        balanceHighscore = Math.Max(balanceHighscore, os);
                        JobModExDB[jobDef.shortHash] = jobExtension;
                        BalanceJob.Add(jobDef.shortHash);
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
                        JobModExDB[jobDef.shortHash] = jobExtension;
                        CardioJob.Add(jobDef.shortHash);
                    }   
                    break;
            }
        }

        public static void RegisterGeneFactors()
        {

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


