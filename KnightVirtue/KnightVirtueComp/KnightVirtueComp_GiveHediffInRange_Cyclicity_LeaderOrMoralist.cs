using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediffInRange_Cyclicity_NonPrimaryIdeoColonist : KnightVirtueComp_GiveHediffInRange_Cyclicity
{
    public override bool HasExtraPawnValiator => ModsConfig.IdeologyActive;

    protected override bool ExtraPawnValiator(Pawn target)
    {
        if (!ModsConfig.IdeologyActive)
            return false;
        Ideo primaryIdeo = Faction.OfPlayer.ideos?.PrimaryIdeo;
        if (primaryIdeo is null)
            return false;

        return primaryIdeo == target.Ideo;
    }

    public override void PostActive()
    {
        if (ModsConfig.IdeologyActive)
            base.PostActive();
    }

    public override void PostRemove()
    {
        if (ModsConfig.IdeologyActive)
            base.PostRemove();
    }
}
public class KnightVirtueComp_GiveHediffInRange_Cyclicity_LeaderOrMoralist : KnightVirtueComp_GiveHediffInRange_Cyclicity
{
    public override bool HasExtraPawnValiator => false;

    public override void PostActive()
    {
        if (ModsConfig.IdeologyActive)
            base.PostActive();
    }

    public override void PostRemove()
    {
        if (ModsConfig.IdeologyActive)
            base.PostRemove();
    }

    public override void TickInterval(int delta)
    {
        if (!this.Pawn.IsHashIntervalTick(Props.checkInterval, delta))
            return;

        Precept_Role role = this.Pawn.Ideo?.GetRole(this.Pawn);
        if (role is null)
            return;

        if (role.def == PreceptDefOf.IdeoRole_Leader || role.def == PreceptDefOf.IdeoRole_Moralist)
            GiveHediffInRange();
    }
}
