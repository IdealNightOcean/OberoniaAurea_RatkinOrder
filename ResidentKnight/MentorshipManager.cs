using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 教导关系管理器 - 负责管理骑士与其授导对象之间的关系，包括添加、移除、查询等功能，并处理相关数据的保存和加载
/// </summary>
public class MentorshipManager : IExposable
{
    public const int MaxStudentsPerKnight = 2;

    public static MentorshipManager Instance { get; private set; }

    private readonly int tickHashOffset;

    [Unsaved] private Dictionary<ResidentPawn, HashSet<ResidentKnight>> studentsToTeachers = [];
    [Unsaved] private Dictionary<ResidentKnight, HashSet<ResidentPawn>> teachersToStudents = [];

    public MentorshipManager()
    {
        OberoniaAurea_Frame.Utility.OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(MentorshipManager));
        Instance = this;

        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }
    public static void ClearStaticCache() => Instance = null;

    public void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            PrepareForSaving();
        }

        Scribe_Collections.Look(ref studentTeacherPairs, nameof(studentTeacherPairs), LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            studentTeacherPairs = null;
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            ConstructFromSavedData();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanAcceptStudent(ResidentKnight knightRecord)
    {
        if (knightRecord is null)
            return false;
        if (!teachersToStudents.TryGetValue(knightRecord, out HashSet<ResidentPawn> students))
            return true;
        return students.Count < MaxStudentsPerKnight;
    }

    public int GetStudentCount(ResidentKnight knightRecord)
    {
        if (knightRecord is null)
            return 0;
        if (teachersToStudents.TryGetValue(knightRecord, out HashSet<ResidentPawn> students))
            return students.Count;
        return 0;
    }

    public bool TryAddStudent(ResidentPawn student, ResidentKnight teacher)
    {
        if (!CanAcceptStudent(teacher))
        {
            Log.Warning($"[OARO] 常驻骑士 {teacher.Pawn.Name} 已达到最大授导对象数量上限 ({MaxStudentsPerKnight})");
            return false;
        }
        return AddStudentDirectly(student, teacher);
    }

    public bool AddStudentDirectly(ResidentPawn student, ResidentKnight teacher)
    {
        if (student is null || teacher is null)
            return false;

        if (!studentsToTeachers.TryGetValue(student, out HashSet<ResidentKnight> teachers))
        {
            studentsToTeachers[student] = [teacher];
        }
        else
        {
            teachers.Add(teacher);
        }
        if (!teachersToStudents.TryGetValue(teacher, out HashSet<ResidentPawn> students))
        {
            teachersToStudents[teacher] = [student];
        }
        else
        {
            students.Add(student);
        }
        return true;
    }

    public bool RemoveTeacher(ResidentKnight teacher)
    {
        if (teacher is null)
            return false;
        if (!teachersToStudents.TryGetValue(teacher, out HashSet<ResidentPawn> students))
            return false;

        foreach (ResidentPawn student in students)
        {
            if (studentsToTeachers.TryGetValue(student, out HashSet<ResidentKnight> teachers))
            {
                teachers.Remove(teacher);
                if (teachers.Count == 0)
                {
                    studentsToTeachers.Remove(student);
                }
            }
        }

        return teachersToStudents.Remove(teacher);
    }

    public bool RemoveStudent(ResidentPawn student)
    {
        if (student is null)
            return false;
        if (!studentsToTeachers.TryGetValue(student, out HashSet<ResidentKnight> teachers))
            return false;

        foreach (ResidentKnight teacher in teachers)
        {
            if (teachersToStudents.TryGetValue(teacher, out HashSet<ResidentPawn> teacherStudents))
            {
                teacherStudents.Remove(student);
                if (teacherStudents.Count == 0)
                {
                    teachersToStudents.Remove(teacher);
                }
            }
        }

        return studentsToTeachers.Remove(student);
    }

    public bool TryGetTeachersOfStudent(ResidentPawn student, out HashSet<ResidentKnight> teachers)
    {
        return studentsToTeachers.TryGetValue(student, out teachers);
    }

    public bool TryGetStudentsOfTeacher(ResidentKnight teacher, out HashSet<ResidentPawn> students)
    {
        return teachersToStudents.TryGetValue(teacher, out students);
    }

    public bool IsStudentOfKnight(ResidentPawn student, ResidentKnight residentKnight)
    {
        if (student is null || residentKnight is null)
            return false;

        if (studentsToTeachers.TryGetValue(student, out HashSet<ResidentKnight> teachers))
        {
            return teachers.Contains(residentKnight);
        }
        return false;
    }

    public void Tick()
    {
        if (TickUtility.IsHashIntervalTick(tickHashOffset, 60000))
        {
            CheckMentorshipVirtueAcquisition();
        }
    }

    public void Notify_ResidentKnightRemoved(ResidentKnight knight)
    {
        RemoveTeacher(knight);
        RemoveStudent(knight);
    }

    public void Notify_ResidentPawnRemoved(ResidentPawn residentPawn)
    {
        RemoveStudent(residentPawn);
    }

    /// <summary>
    /// 每日检测授导美德获取
    /// </summary>
    private void CheckMentorshipVirtueAcquisition()
    {
        foreach (KeyValuePair<ResidentKnight, HashSet<ResidentPawn>> kv in teachersToStudents)
        {
            ResidentKnight teacher = kv.Key;
            if (teacher is null || teacher.VirtueHandler.TotalVirtueCount <= 0)
                continue;

            foreach (ResidentPawn student in kv.Value)
            {
                if (student is not ResidentKnight studentKnight)
                    continue;

                TryAcquireVirtueFromMentorship(teacher, studentKnight);
            }
        }
    }

    /// <summary>
    /// 尝试通过授导获取美德
    /// </summary>
    private void TryAcquireVirtueFromMentorship(ResidentKnight teacher, ResidentKnight student)
    {
        if (teacher is null || student is null)
            return;

        bool isResonate = teacher.IsChivalryResonate(student);
        float virtueChance = isResonate ? 0.1f : 0.04f;
        if (teacher.EffectTags.HasTag(KeyLibrary_EffectTag.ProminentTeacher))
            virtueChance *= 2f;
        if (!Rand.Chance(virtueChance))
            return;

        KnightVirtueDef virtueToTeach = KnightVirtueUtility.GetTeachableVirtues(teacher, student).RandomElementWithFallback(null);
        if (virtueToTeach is null)
            return;

        int level = KnightVirtueUtility.GetRandomNewVirtueLevel_Mentorship(teacher, isResonate);
        string reason = "OARO_KnightVirtueGainReason_Mentorship".Translate(teacher.Pawn.Named(KeyLibrary_FormatArgName.PAWN));
        student.VirtueHandler.TryAddVirtue(virtueToTeach, level, reason);
    }

    private void PrepareForSaving()
    {
        studentTeacherPairs = new List<StudentTeacherPair>(studentsToTeachers.Count);
        foreach ((ResidentPawn student, HashSet<ResidentKnight> teachers) in studentsToTeachers)
        {
            if (student?.Pawn is null)
                continue;

            foreach (ResidentKnight teacher in teachers)
            {
                if (teacher is null)
                    continue;
                StudentTeacherPair stPair = new()
                {
                    student = student,
                    teacher = teacher,
                };
                studentTeacherPairs.Add(stPair);
            }
        }
    }

    private void ConstructFromSavedData()
    {
        studentTeacherPairs?.RemoveAll(pair => pair is null || pair.teacher is null || pair.student is null);

        if (studentTeacherPairs is not null)
        {
            studentsToTeachers = studentTeacherPairs.GroupBy(p => p.student)
                                                    .ToDictionary(g => g.Key,
                                                                  g => g.Select(p => p.teacher).ToHashSet());
            teachersToStudents = studentTeacherPairs.GroupBy(p => p.teacher)
                                                    .ToDictionary(g => g.Key,
                                                                  g => g.Select(p => p.student).ToHashSet());
        }
        else
        {
            studentsToTeachers = [];
            teachersToStudents = [];
        }
        studentTeacherPairs = null;
    }

    private List<StudentTeacherPair> studentTeacherPairs;
    private class StudentTeacherPair : IExposable
    {
        public ResidentPawn student;
        public ResidentKnight teacher;

        public void ExposeData()
        {
            Scribe_References.Look(ref student, nameof(student));
            Scribe_References.Look(ref teacher, nameof(teacher));
        }
    }
}
