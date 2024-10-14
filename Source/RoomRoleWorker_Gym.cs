using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody
{
    internal class RoomRoleWorker_Gym : RoomRoleWorker
    {
        public override float GetScore(Room room)
        {
            var num = 0;
            var containedAndAdjacentThings = room.ContainedAndAdjacentThings;

            for (int i = 0; i < containedAndAdjacentThings.Count; i++)
            {
                Thing thing = containedAndAdjacentThings[i];
                if (thing.def.IsBed && thing.def.building.bed_humanlike)
                {
                    return 0f;
                }
                if (RimbodyDefLists.WorkoutBuilding.Contains(thing.def))
                {
                    num++;
                }
            }

            return num * 8f;
        }
    }
}