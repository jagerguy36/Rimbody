//using RimWorld;
//using System.Collections.Generic;
//using Verse.AI;
//using Verse;

//namespace Maux36.Rimbody
//{
//    internal class JobGiver_GoDoPushUps : ThinkNode_JobGiver
//    {
//        public float maxRadius = -1f;
//        public static List<IntVec3> tmpWorkingCellList = new List<IntVec3>();

//        public static Region ClosestRegionWithinTemperatureRange(IntVec3 root, Map map, Pawn pawn, FloatRange tempRange, TraverseParms traverseParms, RegionType traversableRegionTypes = RegionType.Set_Passable, float maxRadius = -1f)
//        {
//            Region region = root.GetRegion(map, traversableRegionTypes);
//            if (region == null)
//            {
//                return null;
//            }
//            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, isDestination: false);
//            Region foundReg = null;
//            RegionProcessor regionProcessor = delegate (Region r)
//            {
//                if (r.IsDoorway)
//                {
//                    return false;
//                }
//                if (!TryGetAllowedCellInRegion(r, pawn, out var _))
//                {
//                    return false;
//                }
//                if (tempRange.Includes(r.Room.Temperature))
//                {

//                }
//                foundReg = r;
//                return true;
//            };
//            RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 9999, traversableRegionTypes);
//            return foundReg;
//        }

//        public static bool TryGetAllowedCellInRegion(Region region, Pawn pawn, out IntVec3 cell, float maxRadius = -1f)
//        {
//            cell = IntVec3.Invalid;
//            for (int i = 0; i < 100; i++)
//            {
//                IntVec3 randomCell = region.RandomCell;
//                if (randomCell.InAllowedArea(pawn) && (maxRadius < 0f || randomCell.InHorDistOf(pawn.Position, maxRadius)))
//                {
//                    cell = randomCell;
//                    return true;
//                }
//            }
//            foreach (IntVec3 item in region.Cells.InRandomOrder(tmpWorkingCellList))
//            {
//                if (item.InAllowedArea(pawn) && (maxRadius < 0f || item.InHorDistOf(pawn.Position, maxRadius)))
//                {
//                    cell = item;
//                    return true;
//                }
//            }
//            return false;
//        }


//        protected override Job TryGiveJob(Pawn pawn)
//        {
//            Log.Message("Try give job Push-ups");
//            if (pawn.Downed || pawn.Drafted)
//            {
//                return null;
//            }
//            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
//            {
//                return null;
//            }
//            FloatRange tempRange = pawn.ComfortableTemperatureRange();
//            Region region = ClosestRegionWithinTemperatureRange(pawn.Position, pawn.MapHeld, pawn, tempRange, TraverseParms.For(pawn), RegionType.Set_Passable, maxRadius);
//            if (region != null)
//            {
//                TryGetAllowedCellInRegion(region, pawn, out var cell);
//                return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_GoDoPushUps, cell);
//            }
//            return null;
//        }
//    }
//}
