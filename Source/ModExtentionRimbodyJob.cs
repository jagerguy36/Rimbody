using Verse;

namespace Maux36.Rimbody
{
    public class ModExtentionRimbodyJob : DefModExtension
    {
        public float cardio = 1f;
        public float strength = 1f;
    }

    public enum RimbodyBuildingType
    {
        Strength,
        Balance,
        Cardio
    }
    public class ModExtentionRimbodyBuilding : DefModExtension
    {
        public bool faceaway = false;
        public bool isMetal = false;
        public float cardio = 1f;
        public float strength = 1f;
        public RimbodyBuildingType type = RimbodyBuildingType.Balance;
    }
}
