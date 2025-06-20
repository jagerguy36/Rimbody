using CombatExtended;
using Maux36.Rimbody;
using RimWorld;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody_CE
{
    public class Rimbody_CE_Utility
    {
        public static void TryUpdateWeight_CE(ThingOwner owner)
        {
            if (owner?.Owner?.ParentHolder is Pawn pawn)
            {
                TryUpdateInventory_CE(pawn);
            }
        }

        public static void TryUpdateInventory_CE(Pawn pawn)
        {
            if (pawn.needs?.food != null && (Rimbody_Utility.isColonyMember(pawn) || pawn.IsPrisonerOfColony) && Rimbody_Utility.shouldTick(pawn))
            {
                CompPhysique compPhysique = pawn.compPhysique();
                if (compPhysique == null) return;
                if (compPhysique.BodyFat <= -1f || compPhysique.MuscleMass <= -1f) return;
                UpdateCarryweight_CE(pawn, compPhysique);
            }
        }

        public static float GetBaseInventoryCapacity(Pawn pawn)
        {
            return CE_StatDefOf.CarryWeight.defaultBaseValue * pawn.BodySize; //CE for some reason don't use 35f defined in source. Instead uses 40 defined in def.
        }

        private static void UpdateCarryweight_CE(Pawn pawn, CompPhysique compPhysique)
        {
            var compInventory = pawn.TryGetComp<CompInventory>();
            var pawnInventoryCapacity = GetBaseInventoryCapacity(pawn);
            var inventoryWeight = Mathf.Max(0, compInventory.currentWeight - (compInventory.capacityWeight - pawnInventoryCapacity)); // How much burden the pawn is actually under with the inventory stuff

            float capacityWeight = 0f;
            for (int i = 0; i < pawn.carryTracker?.innerContainer?.Count; i++)
            {
                Thing thing = pawn.carryTracker.innerContainer[i];
                capacityWeight += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Mass);
            }

            compPhysique.carryFactor = 0.5f * Mathf.Clamp((inventoryWeight + capacityWeight) / pawnInventoryCapacity, 0f, 2f);
            //Log.Message($"CE: {pawn.Name}'s currentWeight: {compInventory.currentWeight}. Item and movement help: {(compInventory.capacityWeight - pawnInventoryCapacity)}. In total, inventoryWeight: {inventoryWeight} capacityWeight: {capacityWeight} / pawnInventoryCapacity: {pawnInventoryCapacity}. FINAL: {compPhysique.carryFactor}");
        }
    }
}
