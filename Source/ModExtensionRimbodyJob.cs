using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class ModExtensionRimbodyJob : DefModExtension
    {
        public float cardio = 1f;
        public float strength = 1f;
    }

    public enum RimbodyTargetCategory
    {
        Strength,
        Balance,
        Cardio
    }
    public enum RimbodyTargetType
    {
        Building,
        Item
    }
    public class ModExtensionRimbodyTarget : DefModExtension
    {
        public bool faceaway = false;
        public bool isMetal = false;
        public bool useCell = false;
        public float baseEfficiency = 1f;
        public float baseFatigueRate = 1f;
        public float cardio = 1f;
        public float strength = 1f;
        public RimbodyTargetCategory Category = RimbodyTargetCategory.Balance;
        public RimbodyTargetType Type = RimbodyTargetType.Building;
        public Vector3 offset = new Vector3(0,0,0);
    }
}
