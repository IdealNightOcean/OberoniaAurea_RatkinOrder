using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class BombardSupportMaker : Thing
{
    private const int BombInterval = 15;

    private List<IntVec3> CachedAttackPositions { get; } = [];
    private int NextReCacheAttackPositionsTick { get; set; } = -1;

    private int ticksToForceDestroy = 2500;

    private bool bombStart;
    private int ticksToStart = 120;
    private int ticksToNextBomb = 15;

    private int bombardCount;
    private int bombardCountRemaining;

    public void SetBombardCount(Branch branch)
    {
        bombardCount = Mathf.FloorToInt(branch.GetStatValue(BranchStatDefOf.OARO_BombardSupportCeiling));
        bombardCount = bombardCount > 0 ? bombardCount : 1;
        bombardCountRemaining = bombardCount;
        ticksToForceDestroy = bombardCount * BombInterval + 5000;
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if ((ticksToForceDestroy -= delta) <= 0)
        {
            Destroy();
            return;
        }

        if (!bombStart && (ticksToStart -= delta) <= 0)
        {
            ticksToStart = int.MaxValue;
            bombStart = true;
            return;
        }

        if ((ticksToNextBomb -= delta) <= 0)
        {
            ticksToNextBomb = BombInterval;
            DoBombard();
            if (bombardCountRemaining <= 0)
            {
                Destroy();
            }
        }
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        CachedAttackPositions.Clear();
        base.Destroy(mode);
    }

    private void DoBombard()
    {
        if (CachedAttackPositions.Count == 0 || Find.TickManager.TicksGame > NextReCacheAttackPositionsTick)
        {
            RefreshHostileTargetCache();
            if (CachedAttackPositions.Count == 0)
            {
                Destroy();
                return;
            }
        }

        IntVec3 targetCell = CachedAttackPositions.RandomElement();

        ShootLine shootLine = new(Position, targetCell);
        Projectile projectile = (Projectile)GenSpawn.Spawn(OARO_ThingDefOf.OARO_BulletShell_HeavyGrenade, shootLine.Source, Map);
        projectile.Launch(null, Position.ToVector3Shifted(), shootLine.Dest, targetCell, ProjectileHitFlags.NonTargetWorld, preventFriendlyFire: false, equipment: null);
        bombardCountRemaining--;
    }

    private void RefreshHostileTargetCache()
    {
        NextReCacheAttackPositionsTick = Find.TickManager.TicksGame + 120;
        CachedAttackPositions.Clear();
        Map map = Map;
        if (map is null)
        {
            return;
        }

        foreach (IAttackTarget target in map.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer))
        {
            Thing thing = target.Thing;
            if (thing is not null && thing.Spawned)
            {
                CachedAttackPositions.Add(thing.Position);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksToForceDestroy, nameof(ticksToForceDestroy), 0);

        Scribe_Values.Look(ref ticksToStart, nameof(ticksToStart), -1);
        Scribe_Values.Look(ref bombStart, nameof(bombStart), defaultValue: false);
        Scribe_Values.Look(ref ticksToNextBomb, nameof(ticksToNextBomb), 0);

        Scribe_Values.Look(ref bombardCount, nameof(bombardCount), 0);
        Scribe_Values.Look(ref bombardCountRemaining, nameof(bombardCountRemaining), 0);
    }

}