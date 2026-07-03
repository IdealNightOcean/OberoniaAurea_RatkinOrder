using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_KnightVirtue : HediffWithComps
{
    private KnightVirtueHandler virtueHandler;

    private HediffStage buffStage;

    public override HediffStage CurStage => buffStage;

    public override int CurStageIndex => 0;

    public void InitVirtueHandler(KnightVirtueHandler virtueHandler, bool force = false)
    {
        if (virtueHandler is null)
        {
            Log.Error($"[OARO] 尝试使用空的{nameof(KnightVirtueHandler)}对{nameof(Hediff_KnightVirtue)}进行初始化。");
            pawn.health.RemoveHediff(this);
            return;
        }
        if (this.virtueHandler is not null)
        {
            if (!force && this.virtueHandler != virtueHandler)
            {
                Log.Error("[OARO] ");
            }
        }

        this.virtueHandler = virtueHandler;
    }

    public void UpdateBuffStage(HediffStage buffStage) => this.buffStage = buffStage;

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
}