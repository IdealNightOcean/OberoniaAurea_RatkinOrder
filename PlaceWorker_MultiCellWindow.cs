using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class PlaceWorker_MultiCellWindow : PlaceWorker
{
    private static readonly List<IntVec3> TempCells = [];
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        foreach (IntVec3 cell in WallRequirementCells(def, center, rot))
        {
            GhostDrawer.DrawGhostThing(cell, Rot4.South, ThingDefOf.Wall, null, Color.grey, AltitudeLayer.Blueprint, null, drawPlaceWorkers: false);
        }
    }

    public override void PostPlace(Map map, BuildableDef def, IntVec3 loc, Rot4 rot)
    {
        if (def is not ThingDef tDef)
        {
            return;
        }
        foreach (IntVec3 cell in WallRequirementCells(tDef, loc, rot))
        {
            if (!DoorUtility.EncapsulatingWallAt(cell, map, includeUnbuilt: true))
            {
                Messages.Message("MessageBuildingRequiresAdjacentWalls".Translate(def).CapitalizeFirst(), MessageTypeDefOf.CautionInput, historical: false);
                break;
            }
        }
    }

    public static List<IntVec3> WallRequirementCells(ThingDef def, IntVec3 pos, Rot4 rot)
    {
        TempCells.Clear();

        CellRect cellRect = GenAdj.OccupiedRect(IntVec3.Zero, def.defaultPlacingRot, def.size);
        int cellCount = (def.defaultPlacingRot.IsHorizontal ? cellRect.Width : cellRect.Height);
        for (int i = 0; i < cellCount; i++)
        {
            TempCells.Add(pos + new IntVec3(cellRect.minX - 1, 0, cellRect.minZ + i).RotatedBy(rot));
            TempCells.Add(pos + new IntVec3(cellRect.maxX + 1, 0, cellRect.minZ + i).RotatedBy(rot));
        }

        return TempCells;
    }

}
