//using HarmonyLib;
//using Maux36.Rimbody;
//using RimWorld;
//using RimWorld.Planet;
//using System.Collections.Generic;
//using Vehicles;
//using Verse;

//namespace VehicleFrameworkCompatibility.HarmonyPatches
//{

//    [HarmonyPatch(typeof(Vehicles.VehiclePawn), "Notify_Boarded")]
//    public class Notifcy_BoardedPatch
//    {
//        public static void Postfix(ref bool __result, Pawn pawnToBoard)
//        {
//            if (__result)
//            {
//                Log.Message($"{pawnToBoard.Name} boarded");
//                var compPhysique = pawnToBoard.TryGetComp<CompPhysique>();
//                compPhysique.boarded = true;
//            }
//        }
//    }


//    [HarmonyPatch(typeof(Vehicles.VehiclePawn), "Notify_BoardedCaravan")]
//    public class Notify_BoardedCaravanPatch
//    {
//        public static void Postfix(Pawn pawnToBoard)
//        {
//            Log.Message($"{pawnToBoard.Name} boarded caravan");
//            var compPhysique = pawnToBoard.TryGetComp<CompPhysique>();
//            compPhysique.boarded = true;
//        }
//    }


//    [HarmonyPatch(typeof(Vehicles.VehiclePawn), "RemovePawn")]
//    public class RemovePawnPatch
//    {
//        public static void Postfix(Pawn pawn)
//        {
//            Log.Message($"{pawn.Name} exited");
//            var compPhysique = pawn.TryGetComp<CompPhysique>();
//            compPhysique.boarded = false;
//        }
//    }


//    [HarmonyPatch(typeof(Vehicles.VehicleCaravan), "Notify_Merged")]
//    public class Notify_MergedPatch
//    {
//        public static void Postfix(List<Caravan> group)
//        {
//            Log.Message($"caravan merged");

//        }
//    }
//}
