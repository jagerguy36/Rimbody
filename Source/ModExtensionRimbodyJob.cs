using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class ModExtensionRimbodyJob : DefModExtension
    {
        public float cardio = 1f;
        public float strength = 1f;
        public List<int> strengthParts;
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
        Container,
        Item
    }

    public class WorkOut
    {
        public string name;
        public string reportString;
        public float cardio = 1f;
        public float strength = 1f;
        public bool buildingFaceaway = false;
        public bool buildingIsMetal = false;
        public Vector3 itemOffset = new Vector3 (0,0,0);
        public List<int> strengthParts;
    }
    public class ModExtensionRimbodyTarget : DefModExtension
    {

        public RimbodyTargetType Type = RimbodyTargetType.Building;
        public RimbodyTargetCategory Category = RimbodyTargetCategory.Balance;
        public bool buildingUsecell = true;
        public List<WorkOut> workouts = new List<WorkOut>();
    }
}
