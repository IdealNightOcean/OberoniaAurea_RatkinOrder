using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MentorshipManager : IExposable
{
    private ResidentPawnsManager Parent { get; }
    public const int MaxStudentsPerKnight = 2;

    [Unsaved] private Dictionary<ResidentPawn, HashSet<ResidentKnight>> studentsToTeachers = [];
    [Unsaved] private Dictionary<ResidentKnight, HashSet<ResidentPawn>> teachersToStudents = [];

    public MentorshipManager(ResidentPawnsManager parent)
    {
        Parent = parent;
    }

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

    public IReadOnlyCollection<ResidentKnight> GetTeachersOfStudent(ResidentPawn student)
    {
        if (studentsToTeachers.TryGetValue(student, out HashSet<ResidentKnight> teachers))
            return teachers;
        else
            return [];
    }

    public IReadOnlyCollection<ResidentPawn> GetStudentsOfTeacher(ResidentKnight teacher)
    {
        if (teachersToStudents.TryGetValue(teacher, out HashSet<ResidentPawn> students))
            return students;
        else
            return [];
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

    public void TickDay()
    {

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
