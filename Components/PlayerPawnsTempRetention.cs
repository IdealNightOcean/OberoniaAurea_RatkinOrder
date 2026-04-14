using OberoniaAurea_Frame;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class PlayerDespawnedPawnsTempRetention : IExposable
{
    public static PlayerDespawnedPawnsTempRetention Instance { get; private set; }

    private readonly int tickHashOffset;

    private List<Pawn> pawns = [];

    public PlayerDespawnedPawnsTempRetention()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(PlayerDespawnedPawnsTempRetention));
        Instance = this;
        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }
    public static void ClearStaticCache() => Instance = null;

    public void Tick()
    {
        if (!TickUtility.IsHashIntervalTick(tickHashOffset, 15000))
            return;

        if (pawns.NullOrEmpty())
            return;

        pawns.RemoveAll(p => p.DestroyedOrNull() || p.Spawned);

        if (pawns.NullOrEmpty())
            return;

        Map map = Find.RandomPlayerHomeMap;
        if (map is not null)
        {
            TryMakePawnsBackMap(map);
        }
    }

    public void AddPawn(Pawn pawn)
    {
        if (pawn is null || pawn.DestroyedOrNull() || pawn.Spawned)
            return;
        if (!pawns.Contains(pawn))
        {
            pawns.Add(pawn);
        }
    }

    private void TryMakePawnsBackMap(Map map)
    {
        if (!CellFinder.TryRandomClosewalkCellNear(map.Center, map, radius: 200, out IntVec3 spawnCenter))
        {
            return;
        }

        foreach (Pawn pawn in pawns)
        {
            GenSpawn.Spawn(pawn, spawnCenter, map, WipeMode.VanishOrMoveAside);
        }

        pawns.RemoveAll(p => p.DestroyedOrNull() || p.Spawned);
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref pawns, nameof(pawns), LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pawns.RemoveAll(p => p.DestroyedOrNull() || p.Spawned);
        }
    }
}
