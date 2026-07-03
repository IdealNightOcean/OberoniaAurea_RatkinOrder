using OberoniaAurea_Frame;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediff_Justice_Instructor : KnightVirtueComp, ITickInterval
{
    public KnightVirtueCompProperties_HediffGiver Props => (KnightVirtueCompProperties_HediffGiver)props;

    public void TickInterval(int delta)
    {
        if (this.Pawn.IsHashIntervalTick(Props.checkInterval, delta))
        {
            if (ResidentPawnsManager.MentorshipManager.TryGetStudentsOfTeacher(this.Knight, out HashSet<ResidentPawn> students))
            {
                foreach (ResidentPawn student in students)
                {
                    student.Pawn.GetOrAddHediff(Props.giveParams);
                }
            }
        }
    }
}