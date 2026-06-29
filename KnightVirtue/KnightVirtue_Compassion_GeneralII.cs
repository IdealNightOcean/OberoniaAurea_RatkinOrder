using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 援护·通识II
/// </summary>
public class KnightVirtue_Compassion_GeneralII : KnightVirtueWithComps
{
    private int nextAvailableTick = -1;

    public override void Notify_StimulatedBy(KnightRecord initiatorKnight)
    {
        if (initiatorKnight != knight.KnightRecord && Find.TickManager.TicksGame > nextAvailableTick)
        {
            nextAvailableTick = Find.TickManager.TicksGame + 60;
            KnightChivalryUtility.KnightStimulate(knight.KnightRecord, initiatorKnight.Pawn);
        }
    }
}