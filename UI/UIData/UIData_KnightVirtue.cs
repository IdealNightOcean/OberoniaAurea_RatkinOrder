using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_KnightVirtue : UIDataBase
{
    public ResidentKnight Knight { get; }

    public AcademicHandler AcademicHandler { get; }
    public KnightVirtueHandler VirtueHandler { get; }

    public List<UIData_KnightAcademic> academicsUICache;

    public List<UIData_MentorshipStudent> studentUICache;


    public UIData_KnightVirtue(ResidentKnight knight)
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
            academicsUICache.Add(new UIData_KnightAcademic(Knight, academic));
        }

        studentUICache ??= [];
        studentUICache.Clear();
        if (MentorshipManager.Instance.TryGetStudentsOfTeacher(Knight, out HashSet<ResidentPawn> students))
        {
            studentUICache.Capacity = students.Count;
            foreach (ResidentPawn student in students)
            {
                studentUICache.Add(new UIData_MentorshipStudent(Knight, student));
            }
        }
    }

}