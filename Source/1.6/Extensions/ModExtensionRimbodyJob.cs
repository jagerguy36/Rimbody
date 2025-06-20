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
        public RimbodyWorkoutCategory Category = RimbodyWorkoutCategory.Job;
        public RimbodyWorkoutCategory TreatAs = RimbodyWorkoutCategory.Job;
        public RimbodyJobCategory JobCategory = RimbodyJobCategory.None;
    }

    public enum RimbodyWorkoutCategory
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
        Away,
        Center,
        FaceSame,
        FaceOpposite,
        LyingFrontSame,
        LyingFrontOpposite,
        LyingDownSame,
        LyingUpSame,
        Sit
    }
    public enum InteractionType
    {
        Still,
        Melee,
        Building,
        Item,
        ItemEach,
        ItemBoth
    }
    public enum ItemSpot
    {
        None,
        FlatBench,
        ExerciseMats
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
        public RimbodyWorkoutCategory Category = RimbodyWorkoutCategory.Balance;
        public float cardio = 1f;
        public float strength = 1f;
        public bool playSound = false;
        public ItemSpot spot = ItemSpot.None;
        public InteractionType animationType = InteractionType.Still;
        public Direction pawnDirection = Direction.Center;
        public List<float> strengthParts;
        public vectorSet movingpartAnimOffset;
        public vectorSet movingpartAnimPeak;
        public vectorSet pawnAnimOffset;
        public vectorSet pawnAnimPeak;
        public string customWorkoutTickHandler;
    }
    public class ModExtensionRimbodyTarget : DefModExtension
    {

        public RimbodyTargetType Type = RimbodyTargetType.Building;
        public bool moveBase = false;
        public List<GraphicData> rimbodyTargetpartGraphics;
        public List<WorkOut> workouts = new();
    }
}
