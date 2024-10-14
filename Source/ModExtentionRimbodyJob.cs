using Verse;

namespace Maux36.Rimbody
{
    public class ModExtentionRimbodyJob : DefModExtension
    {
        public float cardio = 1f;
        public float strength = 1f;
    }
    public class ModExtentionRimbodyBuilding : DefModExtension
    {
        public bool faceaway = false;
        public bool isMetal = false;
        public float cardio = 1f;
        public float strength = 1f;
        public int type = 0;
        //0 Nothing || Unspecified
        //1 Arms
        //2 Legs
        //3 Chest
        //4 Back
        //5 Abs
        //6 Shoulders
        //7 Balance
        //8 Others
    }
}
