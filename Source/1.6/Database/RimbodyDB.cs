using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public class RimbodyDB
    {
        public static List<ThingDef> StrengthTargets = new();
        public static List<ThingDef> CardioTargets = new();
        public static List<ThingDef> BalanceTargets = new();
        public static List<JobDef> StrengthNontargetJobs = new();
        public static List<JobDef> CardioNontargetJobs = new();
        public static List<JobDef> BalanceNontargetJobs = new();
        public static Dictionary<ushort, ModExtensionRimbodyTarget> ThingModExDB = new();
        public static Dictionary<ushort, ModExtensionRimbodyJob> JobModExDB = new();
        public static Dictionary<ushort, ModExtensionRimbodyJob> GiverModExDB = new();
        public static HashSet<ushort> WorkoutBuildingHash = new();

        public static HashSet<ushort> RunningJobHash = new();
        public static HashSet<ushort> ChunkJobHash = new();
        public static List<float> jogging_parts;
        public static List<float> jogging_parts_jogger;
        //Highscore for workouts without target
        public static float strengthHighscore = 0;
        public static float cardioHighscore = 0;
        public static float balanceHighscore = 0;
        //hash,f_gain, f_lose, m_gain, m_lose, 
        public static HashSet<ushort> ObservedGeneHash = new();
        public static Dictionary<ushort, (float, float, float, float)> GeneFactors = new();
        public static Dictionary<string, int> HarmonyInjectorID = new Dictionary<string, int> {
            { "GiddyUp", 0 },
            { "CombatTraining", 1 },
        };


        static RimbodyDB() // Static constructor
        {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                var buildingExtension = thingDef.GetModExtension<ModExtensionRimbodyTarget>();
                if (buildingExtension != null)
                {
                    AddWorkoutTarget(thingDef, buildingExtension);
                }
            }

            foreach (var jobDef in DefDatabase<JobDef>.AllDefs)
            {
                var jobExtension = jobDef.GetModExtension<ModExtensionRimbodyJob>();
                if(jobExtension != null)
                {
                    if (jobExtension?.Category !=null && jobExtension?.Category != RimbodyWorkoutCategory.Job)
                    {
                        AddWorkoutJob(jobDef, jobExtension);
                    }
                    JobModExDB[jobDef.shortHash] = jobExtension;
                }
            }

            foreach (var giverDef in DefDatabase<WorkGiverDef>.AllDefs)
            {
                var giverExtension = giverDef.GetModExtension<ModExtensionRimbodyJob>();
                if(giverExtension != null)
                {
                    GiverModExDB[giverDef.shortHash] = giverExtension;
                }
            }

            RegisterGeneFactors(GeneFactors);
            if(ModsConfig.BiotechActive)
            {
                foreach (var geneDef in DefDatabase<GeneDef>.AllDefs)
                {
                    if(geneDef.endogeneCategory == EndogeneCategory.BodyType)
                    {
                        ObservedGeneHash.Add(geneDef.shortHash);
                    }
                }
                ObservedGeneHash.Add(DefOf_Rimbody.DiseaseFree.shortHash);
            }

            RunningJobHash.Add(DefOf_Rimbody.Rimbody_Jogging.shortHash);
            if (ModsConfig.BiotechActive)
            {
                RunningJobHash.Add(DefDatabase<JobDef>.GetNamed("NatureRunning").shortHash);
            }

        }

        private static void AddWorkoutTarget(ThingDef targetDef, ModExtensionRimbodyTarget targetExtension)
        {
            foreach (var workout in targetExtension.workouts)
            {
                ThingModExDB[targetDef.shortHash] = targetExtension;
                switch (workout.Category)
                {
                    case RimbodyWorkoutCategory.Strength:
                        StrengthTargets.Add(targetDef);
                        break;
                    case RimbodyWorkoutCategory.Balance:
                        BalanceTargets.Add(targetDef);
                        break;
                    case RimbodyWorkoutCategory.Cardio:
                        CardioTargets.Add(targetDef);
                        break;
                }
            }

            if (targetExtension.Type == RimbodyTargetType.Building)
            {
                WorkoutBuildingHash.Add(targetDef.shortHash);
            }
        }

        private static void AddWorkoutJob(JobDef jobDef, ModExtensionRimbodyJob jobExtension)
        {
            switch (jobExtension.Category)
            {
                case RimbodyWorkoutCategory.Strength:
                    if (jobExtension.strengthParts != null)
                    {
                        var os = GetOptimalStrengthPartScore(jobExtension.strengthParts, jobExtension.strength);
                        strengthHighscore = Math.Max(strengthHighscore, os);
                        StrengthNontargetJobs.Add(jobDef);
                        if(jobDef.defName.StartsWith("Rimbody_DoChunk"))
                            ChunkJobHash.Add(jobDef.shortHash);
                    }
                    break;
                case RimbodyWorkoutCategory.Balance:
                    if (jobExtension.strengthParts != null)
                    {
                        var os = GetOptimalStrengthPartScore(jobExtension.strengthParts, jobExtension.strength);
                        balanceHighscore = Math.Max(balanceHighscore, os);
                        BalanceNontargetJobs.Add(jobDef);
                    }
                    break;
                case RimbodyWorkoutCategory.Cardio:
                    if (jobExtension.strengthParts != null)
                    {
                        if(jobDef == DefOf_Rimbody.Rimbody_Jogging)
                        {
                            jogging_parts = jobExtension.strengthParts;
                            jogging_parts_jogger = jobExtension.strengthParts.Select(x => x * 0.5f).ToList();
                        }
                        cardioHighscore = Math.Max(cardioHighscore, GetOptimalCardioJobScore(jobExtension.strengthParts, jobExtension.cardio));
                        CardioNontargetJobs.Add(jobDef);
                    }   
                    break;
            }
        }

        public static void RegisterGeneFactors(Dictionary<ushort, (float, float, float, float)> GeneFactors)
        {
            if (!ModsConfig.BiotechActive) return;
            //f_gain, f_lose, m_gain, m_lose, 
            GeneDef geneDef;
            geneDef = DefOf_Rimbody.Body_Fat;
            if (geneDef != null) GeneFactors[geneDef.shortHash] = (1.25f, 0.85f, 1f, 1f);
            geneDef = DefOf_Rimbody.Body_Thin;
            if (geneDef != null) GeneFactors[geneDef.shortHash] = (0.75f, 1.15f, 1f, 1f);
            geneDef = DefOf_Rimbody.Body_Hulk;
            if (geneDef != null) GeneFactors[geneDef.shortHash] = (1f, 1f, 1.25f, 0.75f);
            geneDef = DefOf_Rimbody.Body_Standard;
            if (geneDef != null) GeneFactors[geneDef.shortHash] = (0.85f, 1f, 1.15f, 1f);
            return;
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


