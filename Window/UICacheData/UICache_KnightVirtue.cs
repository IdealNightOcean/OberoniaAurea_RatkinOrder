using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder.UI;

public class UICache_KnightVirtue : UICacheBase
{
    public ResidentKnight Knight { get; }

    public AcademicHandler AcademicHandler { get; }
    public KnightVirtueHandler VirtueHandler { get; }

    public List<UICache_KnightAcademic> academicsUICache;

    public List<UICache_MentorshipStudent> studentUICache;


    public UICache_KnightVirtue(ResidentKnight knight)
    {
        this.Knight = knight;
        this.VirtueHandler = knight.VirtueHandler;
        this.AcademicHandler = knight.AcademicHandler;
    }


    protected override void RefreshInner()
    {
        academicsUICache ??= new(AcademicHandler.Academics.Count);
        academicsUICache.Clear();
        foreach (KnightAcademicDef academic in AcademicHandler.Academics.Keys)
        {
            academicsUICache.Add(new UICache_KnightAcademic(Knight, academic));
        }

        studentUICache ??= [];
        studentUICache.Clear();
        if (MentorshipManager.Instance.TryGetStudentsOfTeacher(Knight, out HashSet<ResidentPawn> students))
        {
            studentUICache.Capacity = students.Count;
            foreach (ResidentPawn student in students)
            {
                studentUICache.Add(new UICache_MentorshipStudent(Knight, student));
            }
        }
    }

}