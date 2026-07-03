using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class KnightChivalryUtility
{
    /// <summary>
    /// 检查两个骑士精神是否共鸣
    /// </summary>
    public static bool IsChivalryResonate(this KnightChivalryDef chivalry, KnightChivalryDef other)
    {
        if (chivalry is null || other is null)
            return false;
        if (chivalry == other)
            return true;
        if (chivalry.ResonateChivalriesSet.Contains(other))
            return true;
        return false;
    }

    /// <summary>
    /// 检查两个骑士是否精神共鸣
    /// </summary>
    public static bool IsChivalryResonate(this KnightRecord knight, KnightRecord other)
    {
        if (knight is null || other is null)
            return false;

        KnightChivalryDef knightChivalry = knight.Chivalry;
        KnightChivalryDef otherChivalry = other.Chivalry;

        return IsChivalryResonate(knightChivalry, otherChivalry);
    }

    /// <summary>
    /// 检查两个常驻骑士是否精神共鸣
    /// </summary>
    public static bool IsChivalryResonate(this ResidentKnight knight, ResidentKnight other)
    {
        if (knight is null || other is null)
            return false;

        KnightChivalryDef knightChivalry = knight.Chivalry;
        KnightChivalryDef otherChivalry = other.Chivalry;

        return IsChivalryResonate(knightChivalry, otherChivalry);
    }

    public static void KnightStimulate(KnightRecord initiatorKnight, Pawn recipient)
    {
        if (recipient.DestroyedOrNull())
            return;

        HediffDef knightlyTalkHediff = initiatorKnight.Chivalry?.stimulateHediff;
        if (knightlyTalkHediff is null)
            return;

        Hediff hediff = recipient.health.GetOrAddHediff(knightlyTalkHediff);
        HediffComp_Disappears disappearsComp = hediff.TryGetComp<HediffComp_Disappears>();
        if (disappearsComp is not null)
        {
            disappearsComp.disappearsAfterTicks = 5 * 60000;
            disappearsComp.ticksToDisappear = 5 * 60000;
        }

        if (ResidentPawnsManager.Instance.TryGetKnightRecord(recipient, out ResidentKnight initiatorResidentKnight))
            initiatorResidentKnight.Notify_Stimulate(recipient);

        if (ResidentPawnsManager.Instance.TryGetKnightRecord(recipient, out ResidentKnight recipientResidentKnight))
            recipientResidentKnight.Notify_StimulatedBy(initiatorKnight);
    }
}