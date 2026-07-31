using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class PlayerDespawnedPawnsTempRetention : IExposable, IPawnRetentionHolder, IThingHolder
{
    public static PlayerDespawnedPawnsTempRetention Instance { get; private set; }

    private readonly int tickHashOffset;
    private ThingOwner<Pawn> pawns;

    public PlayerDespawnedPawnsTempRetention()
    {
        OberoniaAurea_Frame.Utility.OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(PlayerDespawnedPawnsTempRetention));
        Instance = this;
        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
        pawns = new();
    }
    public static void ClearStaticCache() => Instance = null;

    public void Tick()
    {
        if (!TickUtility.IsHashIntervalTick(tickHashOffset, 15000))
            return;

        if (!pawns.Any)
            return;

        pawns.RemoveAll(p => p.DestroyedOrNull() || p.Spawned);

        if (!pawns.Any)
            return;

        Map map = Find.RandomPlayerHomeMap;
        if (map is not null)
        {
            TryMakePawnsBackMap(map);
        }
    }

    public void AddPawn(Pawn pawn)
    {
        if (pawn.DestroyedOrNull() || pawn.Spawned)
            return;

        if (!pawns.Contains(pawn))
        {
            pawns.TryAddOrTransfer(pawn);
        }
    }

    private void TryMakePawnsBackMap(Map map)
    {
        if (!CellFinder.TryRandomClosewalkCellNear(map.Center, map, radius: 200, out IntVec3 spawnCenter))
        {
            return;
        }

        List<Pawn> pawnsForModify = [.. pawns.InnerListForReading];
        foreach (Pawn pawn in pawnsForModify)
        {
            GenSpawn.Spawn(pawn, spawnCenter, map, WipeMode.VanishOrMoveAside);
        }

        pawns.RemoveAll(p => p.DestroyedOrNull() || p.Spawned);
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref pawns, nameof(pawns));
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pawns.RemoveAll(p => p.DestroyedOrNull() || p.Spawned);
        }
    }

    public IThingHolder ParentHolder => null;
    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    public ThingOwner GetDirectlyHeldThings() => pawns;
}
