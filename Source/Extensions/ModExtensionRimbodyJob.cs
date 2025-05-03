using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class ModExtensionRimbodyJob : DefModExtension
    {
        public float cardio = 0.3f;
        public float strength = 0.1f;
        public List<float> strengthParts;
        public RimbodyTargetCategory Category = RimbodyTargetCategory.Job;
        public RimbodyTargetCategory TreatAs = RimbodyTargetCategory.Job;
        public RimbodyJobCategory JobCategory = RimbodyJobCategory.None;
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
    public enum RimbodyJobCategory
    {
        None,
        Melee,
        HardLabor,
        NormalLabor,
        LightLabor,
        Activity,
        Base,
        Rest
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
        still,
        melee,
        building,
        item,
        itemEach,
        itemBoth
    }
    public class vectorSet
    {
        public Vector3 north = Vector3.zero;
        public Vector3 east = Vector3.zero;
        public Vector3 south = Vector3.zero;
        public Vector3 west = Vector3.zero;

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
        public List<GraphicData> rimbodyTargetpartGraphics;
        public List<WorkOut> workouts = new();
    }
}
