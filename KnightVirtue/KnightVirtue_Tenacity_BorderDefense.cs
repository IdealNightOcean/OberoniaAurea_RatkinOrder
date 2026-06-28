using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 坚毅·边防
/// </summary>
public class KnightVirtue_Tenacity_BorderDefense : KnightVirtueWithComps, ITickInterval
{
    public void TickInterval(int delta)
    {
        if (!this.Pawn.IsHashIntervalTick(60000, delta))
            return;

        foreach (ResidentPawn student in ResidentPawnsManager.MentorshipManager.GetStudentsOfTeacher(knight))
        {
            KnightChivalryUtility.KnightlyTalkStimulate(knight.KnightRecord, student.Pawn);
        }
    }
}
