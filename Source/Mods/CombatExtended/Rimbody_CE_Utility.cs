using CombatExtended;
using Maux36.Rimbody;
using RimWorld;
using RimWorld.Planet;
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
            if (pawn?.needs != null && pawn.needs.food != null && (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || pawn.IsColonist && pawn.GetCaravan() != null))
            {
                CompPhysique compPhysique = pawn.TryGetComp<CompPhysique>();
                if (compPhysique != null)
                {
                    UpdateCarryweight_CE(pawn, compPhysique);
                }
            }
        }

        public static float GetBaseInventoryCapacity(Pawn pawn)
        {
            return 40f * pawn.BodySize;
        }

        private static void UpdateCarryweight_CE(Pawn pawn, CompPhysique compPhysique)
        {
            var compInventory = pawn.TryGetComp<CompInventory>();
            var pawnInventoryCapacity = GetBaseInventoryCapacity(pawn);
            var inventoryWeight = Mathf.Max(0, compInventory.currentWeight - (compInventory.capacityWeight - pawnInventoryCapacity));

            float capacityWeight = 0f;
            for (int i = 0; i < pawn.carryTracker.innerContainer.Count; i++)
            {
                Thing thing = pawn.carryTracker.innerContainer[i];
                capacityWeight += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Mass);
            }
            compPhysique.carryFactor = Mathf.Clamp((inventoryWeight + capacityWeight) / (pawnInventoryCapacity + pawn.GetStatValue(StatDefOf.CarryingCapacity)), 0f, 1f);
        }
    }
}
