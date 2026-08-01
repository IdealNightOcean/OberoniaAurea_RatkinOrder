using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_KnightVirtue : HediffWithComps
{
    private ResidentKnight knight;
    private KnightVirtueHandler virtueHandler;

    private HediffStage buffStage;

    public override HediffStage CurStage => buffStage ??= virtueHandler?.GetNewBuffStage();

    public override int CurStageIndex => 0;

    public void InitVirtueHandler(ResidentKnight knight, bool force = false)
    {
        if (knight is null)
        {
            Log.Error($"[OARO] 尝试使用空的{nameof(KnightVirtueHandler)}对{nameof(Hediff_KnightVirtue)}进行初始化。");
            pawn.health.RemoveHediff(this);
            return;
        }
        if (this.knight is not null)
        {
            if (!force && this.knight != knight)
            {
                Log.Error($"[OARO] {nameof(Hediff_KnightVirtue)}已绑定骑士 {this.knight?.Pawn?.ToString() ?? "UNKNOWN"}，尝试以不同骑士 {knight?.Pawn?.ToString() ?? "UNKNOWN"} 重新初始化。如需覆盖请使用强制模式。");
            }
        }

        this.knight = knight;
        this.virtueHandler = knight.VirtueHandler;
    }

    public void ClearBuffStage() => buffStage = null;

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        virtueHandler?.TickInterval(delta);
    }

    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        base.Notify_KilledPawn(victim, dinfo);
        virtueHandler?.Notify_KilledPawn(victim, dinfo);
    }

    public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
        virtueHandler?.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref knight, nameof(knight));
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (knight is null)
            {
                Log.Error($"[OARO] 在加载后初始化阶段，{nameof(Hediff_KnightVirtue)}的{nameof(knight)}为null。");
                pawn.health.RemoveHediff(this);
                return;
            }
            virtueHandler = knight.VirtueHandler;
        }
    }
}