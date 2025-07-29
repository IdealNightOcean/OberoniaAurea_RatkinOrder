using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class BombardSupportMaker : ThingWithComps
{
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
        this.bombardCount = count;
        this.bombardCountRemaining = count;
    }

    public override void Tick()
    {
        base.Tick();

        ticksToForceDestroy--;
        if (ticksToForceDestroy < 0)
        {
            Destroy();
            return;
        }

        if (bombStart)
        {
            bombInterval--;
            if (bombInterval == 0)
            {
                bombInterval = 15;
                if (curRound < targetCells.Count)
                {
                    DoBombard(Map, targetCells[curRound++], roundBombCount);
                    bombardCountRemaining -= roundBombCount;
                }
                else if (bombardCountRemaining > 0)
                {
                    DoBombard(Map, targetCells.Last(), bombardCountRemaining);
                    bombardCountRemaining = 0;
                }
                else
                {
                    Destroy();
                }
            }
        }
        else
        {
            ticksToStart--;
            if (ticksToStart == 0)
            {
                StartBomb();
            }
        }
    }

    private void StartBomb()
    {
        Map map = base.Map;
        if (map is null || bombardCount <= 0)
        {
            Destroy();
            return;
        }

        HashSet<IAttackTarget> attackTargets = map.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer);

        if (attackTargets.NullOrEmpty())
        {
            Destroy();
            return;
        }
        targetCells = attackTargets.Take(bombardCount).Where(at => at.Thing is not null).Select(at => at.Thing.PositionHeld).ToList();
        roundBombCount = bombardCount / targetCells.Count;
        bombStart = true;
    }


    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        targetCells.Clear();
        base.Destroy(mode);
    }

    private void DoBombard(Map map, IntVec3 cell, int bombCount)
    {
        if (!cell.IsValid)
        {
            return;
        }

        IntVec3 bombSource = new IntVec3(0, 30, 0);
        ShootLine shootLine = new(bombSource, cell);

        for (int i = 0; i < bombardCount; i++)
        {
            Projectile projectile = (Projectile)GenSpawn.Spawn(OARO_ModDefOf.Bullet_Shell_HighExplosive, shootLine.Source, map);
            projectile.Launch(null, bombSource.ToVector3Shifted(), shootLine.Dest, cell, ProjectileHitFlags.NonTargetWorld, preventFriendlyFire: false, equipment: null);
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
