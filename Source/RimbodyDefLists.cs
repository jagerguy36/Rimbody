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
        public static Dictionary<JobDef, ModExtensionRimbodyJob> StrengthNonTargetJob = new();
        public static Dictionary<JobDef, ModExtensionRimbodyJob> CardioNonTargetJob = new();
        public static Dictionary<JobDef, ModExtensionRimbodyJob> BalanceNonTargetJob = new();
        public static HashSet<string> StrengthJob = new HashSet<string>{ "Rimbody_DoStrengthLifting", "Rimbody_DoStrengthBuilding" };
        public static HashSet<string> CardioJob = new HashSet<string> { "Rimbody_DoCardioBuilding" };
        public static HashSet<string> BalanceJob = new HashSet<string> { "Rimbody_DoBalanceBuilding" };
        public static List<float> jogging_parts;
        public static List<float> jogging_parts_jogger;
        public static float strengthHighscore = 0;
        public static float cardioHighscore = 0;
        public static float balanceHighscore = 0;

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
                if (jobExtension?.Category !=null && jobExtension?.Category != RimbodyTargetCategory.Job)
                {
                    AddJob(jobDef, jobExtension);
                }
            }
        }

        private static void AddTarget(ThingDef targetDef, ModExtensionRimbodyTarget targetExtension)
        {
            switch (targetExtension.Category)
            {
                case RimbodyTargetCategory.Strength:
                    StrengthTarget[targetDef] = targetExtension;
                    break;
                case RimbodyTargetCategory.Balance:
                    BalanceTarget[targetDef] = targetExtension;
                    break;
                case RimbodyTargetCategory.Cardio:
                    CardioTarget[targetDef] = targetExtension;
                    break;
            }

            if(targetExtension.Type == RimbodyTargetType.Building)
            {
                WorkoutBuilding[targetDef] = targetExtension;
            }

        }
        private static void AddJob(JobDef jobDef, ModExtensionRimbodyJob jobExtension)
        {
            switch (jobExtension.Category)
            {
                case RimbodyTargetCategory.Strength:
                    if (jobExtension.strengthParts != null)
                    {
                        var os = GetOptimalStrengthPartScore(jobExtension.strengthParts, jobExtension.strength);
                        strengthHighscore = Math.Max(strengthHighscore, os);
                        StrengthNonTargetJob[jobDef] = jobExtension;
                        StrengthJob.Add(jobDef.defName);
                    }
                    break;
                case RimbodyTargetCategory.Balance:
                    if (jobExtension.strengthParts != null)
                    {
                        var os = GetOptimalStrengthPartScore(jobExtension.strengthParts, jobExtension.strength);
                        balanceHighscore = Math.Max(balanceHighscore, os);
                        BalanceNonTargetJob[jobDef] = jobExtension;
                        BalanceJob.Add(jobDef.defName);
                    }
                    break;
                case RimbodyTargetCategory.Cardio:
                    if (jobExtension.strengthParts != null)
                    {
                        if(jobDef.defName == "Rimbody_Jogging")
                        {
                            jogging_parts = jobExtension.strengthParts;
                            jogging_parts_jogger = jobExtension.strengthParts.Select(x => x * 0.5f).ToList();
                        }
                        cardioHighscore = Math.Max(cardioHighscore, jobExtension.cardio);
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
    }
}


