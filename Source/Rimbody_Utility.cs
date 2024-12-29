using LudeonTK;
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

        [DebugAction("Pawns", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
        public static void ResetRimbodyValue(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (compPhysique != null)
            {
                Log.Message($"resetting rimbody for pawn {pawn.Name}");
                compPhysique.PhysiqueValueSetup(true);
            }

        }
    }
}
