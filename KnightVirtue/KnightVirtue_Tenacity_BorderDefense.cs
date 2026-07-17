using System.Collections.Generic;
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

        if (MentorshipManager.Instance.TryGetStudentsOfTeacher(knight, out HashSet<ResidentPawn> students))
        {
            foreach (ResidentPawn student in students)
            {
                KnightChivalryUtility.KnightStimulate(knight.KnightRecord, student.Pawn);
            }
        }
    }
}