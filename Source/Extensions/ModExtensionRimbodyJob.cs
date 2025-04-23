using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class ModExtensionRimbodyJob : DefModExtension
    {
        public float cardio = 1f;
        public float strength = 1f;
        public RimbodyTargetCategory Category = RimbodyTargetCategory.Job;
        public List<float> strengthParts;
    }

    public enum RimbodyTargetCategory
    {
        Strength,
        Balance,
        Cardio,
        Job
    }
    public enum RimbodyTargetType
    {
        Building,
        Item
    }
    public enum Direction
    {
        away,
        center,
        faceSame,
        faceOpposite,
        lyingFrontSame,
        lyingFrontOpposite,
        lyingDownSame,
        lyingUpSame

    }
    public enum InteractionType
    {
        melee,
        animation,
        still
    }
    public class vectorSet
    {
        public Vector3 north = new Vector3(0f, 0f, 0f);
        public Vector3 east = new Vector3(0f, 0f, 0f);
        public Vector3 south = new Vector3(0f, 0f, 0f);
        public Vector3 west = new Vector3(0f, 0f, 0f);

        public Vector3 FromRot(Rot4 rot)
        {
            return rot.AsInt switch
            {
                0 => north,
                1 => east,
                2 => south,
                3 => west,
                _ => Vector3.zero
            };
        }

    }
    public class WorkOut
    {
        public string name;
        public string reportString;
        public float cardio = 1f;
        public float strength = 1f;
        public bool playSound = false;
        public bool useBench = false;
        public InteractionType animationType = InteractionType.still;
        public Direction pawnDirection = Direction.center;
        public List<float> strengthParts;
        public vectorSet movingpartAnimOffset;
        public vectorSet movingpartAnimPeak;
        public vectorSet pawnAnimOffset;
        public vectorSet pawnAnimPeak;
    }
    public class ModExtensionRimbodyTarget : DefModExtension
    {

        public RimbodyTargetType Type = RimbodyTargetType.Building;
        public RimbodyTargetCategory Category = RimbodyTargetCategory.Balance;
        public bool moveBase = false;
        public List<GraphicData> rimbodyBuildingpartGraphics;
        public List<WorkOut> workouts = new();
    }
}
