namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 援护·通识II
/// </summary>
public class KnightVirtue_Compassion_GeneralII : KnightVirtueWithComps
{
    public override void Notify_StimulatedBy(KnightRecord initiatorKnight)
    {
        if (initiatorKnight != knight.KnightRecord)
        {
            KnightChivalryUtility.KnightlyTalkStimulate(knight.KnightRecord, initiatorKnight.Pawn);
        }
    }
}
