using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class BombardSupportMaker : ThingWithComps
{
    private List<IntVec3> CachedAttackPositions { get; } = [];
    private int NextReCacheAttackPositionsTick { get; set; } = -1;

    private int ticksToForceDestroy = 2500;

    private List<IntVec3> targetCells = [];

    private bool bombStart;
    private int ticksToStart = 120;

    private int bombardCount = 20;
    private int bombardCountRemaining = 20;
    private int bombInterval = 15;

    private int roundBombCount = 1;
    private int curRound = 0;

    public void SetBombardCount(int count)
    {
        bombardCount = count;
        bombardCountRemaining = count;
        ticksToForceDestroy = bombardCount * 15 + 5000;
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

        if ((bombInterval -= delta) <= 0)
        {
            bombInterval = 15;
            DoBombard();
            if (bombardCountRemaining <= 0)
            {
                Destroy();
            }
        }
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        targetCells.Clear();
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

        IntVec3 bombSource = new(0, 30, 0);
        ShootLine shootLine = new(bombSource, targetCell);
        Projectile projectile = (Projectile)GenSpawn.Spawn(OARO_ThingDefOf.Bullet_Shell_HighExplosive, shootLine.Source, Map);
        projectile.Launch(null, bombSource.ToVector3Shifted(), shootLine.Dest, targetCell, ProjectileHitFlags.NonTargetWorld, preventFriendlyFire: false, equipment: null);
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
        Scribe_Values.Look(ref ticksToForceDestroy, "ticksToForceDestroy", 0);

        Scribe_Values.Look(ref ticksToStart, "ticksToStart", -1);
        Scribe_Values.Look(ref bombStart, "bombStart", defaultValue: false);
        Scribe_Values.Look(ref bombInterval, "bombInterval", 0);

        Scribe_Values.Look(ref bombardCount, "bombardCount", 0);
        Scribe_Values.Look(ref bombardCountRemaining, "bombardCountRemaining", 0);
        Scribe_Values.Look(ref roundBombCount, "roundBombCount", 0);
        Scribe_Values.Look(ref curRound, "curRound", 0);

        Scribe_Collections.Look(ref targetCells, "targetCells", LookMode.Value);
    }

}