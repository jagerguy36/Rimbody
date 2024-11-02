using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class Rimbody_Utility
    {
        public static void TryUpdateWeight(ThingOwner owner)
        {
            if (owner?.Owner?.ParentHolder is Pawn pawn)
            {
                TryUpdateInventory(pawn);
            }
        }

        public static void TryUpdateInventory(Pawn pawn)
        {
            if (pawn?.needs != null && pawn.needs.food != null && (pawn.IsColonistPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || pawn.IsColonist && pawn.GetCaravan() != null))
            {
                CompPhysique compPhysique = pawn.TryGetComp<CompPhysique>();
                if (compPhysique != null)
                {
                    compPhysique.UpdateCarryweight();
                }
            }
        }
    }
}
