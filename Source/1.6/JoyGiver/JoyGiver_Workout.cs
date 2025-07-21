using RimWorld;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JoyGiver_Workout : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (pawn != null && pawn.ageTracker?.CurLifeStage?.developmentalStage == DevelopmentalStage.Adult)
            {
                if (pawn.needs?.rest?.CurLevel < 0.17f) //Too tired
                {
                    return null;
                }
                if (pawn.IsColonist || pawn.IsPrisonerOfColony)
                {
                    if (compPhysique == null)
                    {
                        return null;
                    }
                    if (RimbodySettings.useExhaustion && compPhysique.resting)
                    {
                        return null;
                    }
                    if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        return null;
                    }
                }
            }

            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return null;
            }
            float sP = 0f;
            float bP = 0f;
            float cP = 0f;
            if (compPhysique.gain >= compPhysique.gainMax * RimbodySettings.gainMaxGracePeriod)
            {
                sP = 0f;
            }
            return JobMaker.MakeJob(def.jobDef, result);
        }
    }
}
