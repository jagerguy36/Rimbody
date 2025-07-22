using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class Rimbody_Utility
    {
        public static bool isColonyMember(Pawn pawn)
        {
            if (pawn.Faction != null && pawn.Faction.IsPlayer && pawn.RaceProps.Humanlike && !pawn.IsMutant) //The same as isColonist Check minus the slave check
            {
                return true;
            }
            return false;
        }

        public static bool shouldTick(Pawn pawn)
        {
            if (pawn.SpawnedOrAnyParentSpawned || pawn.IsCaravanMember() || PawnUtility.IsTravelingInTransportPodWorldObject(pawn))
            {
                return true;
            }
            return false;
        }

        public static bool TooTired(Pawn actor)
        {
            if (((actor != null) & (actor.needs != null)) && actor.needs.rest != null && (double)actor.needs.rest.CurLevel < 0.17f)
            {
                return true;
            }
            return false;
        }

        public static void TryUpdateWeight(ThingOwner owner)
        {
            if (owner?.Owner?.ParentHolder is Pawn pawn)
            {
                TryUpdateInventory(pawn);
            }
        }
        public static float GetBaseInventoryCapacity(Pawn pawn)
        {
            return pawn.BodySize * 35f;//MassUtility.Capacity(pawn); // avoid returning 0
        }

        public static void TryUpdateInventory(Pawn pawn)
        {
            if (pawn.needs?.food != null && (isColonyMember(pawn) || pawn.IsPrisonerOfColony) && shouldTick(pawn))
            {
                CompPhysique compPhysique = pawn.compPhysique();
                if (compPhysique == null) return;
                if (compPhysique.BodyFat <= -1f || compPhysique.MuscleMass <= -1f) return;
                compPhysique.UpdateCarryweight();
            }
        }

        public static IntVec3 FindWorkoutSpot(Pawn actor, bool lookForSeat, ThingDef seatDef, out Thing foundSeat, int maxPawns = 1, float maxDistance = 20f)
        {
            foundSeat = null;
            IntVec3 workoutLocation = IntVec3.Invalid;
            if (lookForSeat)
            {
                Thing thing = null;
                Predicate<Thing> baseChairValidator = delegate (Thing t)
                {
                    if (t.def.building == null) return false;
                    if (t.IsForbidden(actor)) return false;
                    if (!t.IsSociallyProper(actor)) return false;
                    if (t.IsBurning()) return false;
                    if (!TryFindFreeSittingSpotOnThing(t, actor, out var cell)) return false;
                    if (!actor.CanReserve(cell, maxPawns)) return false;
                    return true;
                };
                thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForDef(seatDef), PathEndMode.OnCell, TraverseParms.For(actor), maxDistance, (Thing t) => baseChairValidator(t) && t.Position.GetDangerFor(actor, t.Map) == Danger.None);
                if (thing != null && TryFindFreeSittingSpotOnThing(thing, actor, out workoutLocation))
                {
                    foundSeat = thing;
                    return workoutLocation;
                }
            }
            workoutLocation = RCellFinder.SpotToStandDuringJob(extraValidator: delegate (IntVec3 c)
            {
                if (!actor.CanReserve(c)) return false;
                if (!c.Standable(actor.Map)) return false;
                if (c.GetRegion(actor.Map).type == RegionType.Portal) return false;
                return true;
            }, pawn: actor);
            return workoutLocation;
        }

        public static bool TryFindFreeSittingSpotOnThing(Thing t, Pawn pawn, out IntVec3 cell)
        {
            foreach (IntVec3 item in t.OccupiedRect())
            {
                if (pawn.CanReserve(item, 1, -1, null, false)) //(pawn.CanReserveSittableOrSpot(item))
                {
                    cell = item;
                    return true;
                }
            }
            cell = default;
            return false;
        }


        [DebugAction("Pawns", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
        public static void LogRimbodyValue(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (compPhysique != null)
            {
                Log.Message($"Rimbody for pawn {pawn.Name}\n\n BodyFat: {compPhysique.BodyFat}\n MuscleMass: {compPhysique.MuscleMass}\n isNonSen: {compPhysique.isNonSen}\n FatGainFactor: {compPhysique.FatGainFactor}\n FatLoseFactor: {compPhysique.FatLoseFactor}\n MuscleGainFactor: {compPhysique.MuscleGainFactor}\n MuscleLoseFactor: {compPhysique.MuscleLoseFactor}");
            }

        }

        [DebugAction("Pawns", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
        public static void ResetRimbodyValue(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (compPhysique != null)
            {
                Log.Message($"resetting rimbody for pawn {pawn.Name}");
                compPhysique.PhysiqueValueSetup(true);
                compPhysique.partFatigue = Enumerable.Repeat(0f, RimbodySettings.PartCount).ToList();
            }

        }
    }
}
