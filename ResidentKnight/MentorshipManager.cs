using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MentorshipManager : IExposable
{
    public const int MaxStudentsPerKnight = 2;

    [Unsaved] private Dictionary<Pawn, HashSet<ResidentKnight>> studentsToTeachers = [];
    [Unsaved] private Dictionary<ResidentKnight, HashSet<Pawn>> teachersToStudents = [];

    private List<StudentTeacherPair> studentTeacherPairs;

    public MentorshipManager() { }

    public void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            studentTeacherPairs = new List<StudentTeacherPair>(studentsToTeachers.Count);
            foreach (KeyValuePair<Pawn, HashSet<ResidentKnight>> kv in studentsToTeachers)
            {
                Pawn student = kv.Key;
                if (student is null)
                    continue;

                foreach (ResidentKnight record in kv.Value)
                {
                    if (record is null)
                        continue;
                    StudentTeacherPair stPair = new()
                    {
                        student = student,
                        teacher = record,
                    };
                    studentTeacherPairs.Add(stPair);
                }
            }
        }

        Scribe_Collections.Look(ref studentTeacherPairs, nameof(studentTeacherPairs), LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            studentTeacherPairs = null;
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanAcceptStudent(ResidentKnight knightRecord)
    {
        if (knightRecord is null)
            return false;
        if (!teachersToStudents.TryGetValue(knightRecord, out HashSet<Pawn> students))
            return true;
        return students.Count < MaxStudentsPerKnight;
    }

    public int GetStudentCount(ResidentKnight knightRecord)
    {
        if (knightRecord is null)
            return 0;
        if (teachersToStudents.TryGetValue(knightRecord, out HashSet<Pawn> students))
            return students.Count;
        return 0;
    }

    public bool AddStudent(Pawn studentPawn, ResidentKnight teacherRecord)
    {
        if (studentPawn is null || teacherRecord is null)
            return false;
        if (!CanAcceptStudent(teacherRecord))
        {
            Log.Warning($"[OARO] 常驻骑士 {teacherRecord.Pawn.Name} 已达到最大授导对象数量上限 ({MaxStudentsPerKnight})");
            return false;
        }
        if (!studentsToTeachers.TryGetValue(studentPawn, out HashSet<ResidentKnight> teachers))
        {
            studentsToTeachers[studentPawn] = [teacherRecord];
        }
        else
        {
            teachers.Add(teacherRecord);
        }
        if (!teachersToStudents.TryGetValue(teacherRecord, out HashSet<Pawn> students))
        {
            teachersToStudents[teacherRecord] = [studentPawn];
        }
        else
        {
            students.Add(studentPawn);
        }
        return true;
    }

    public bool RemoveTeacher(ResidentKnight teacher)
    {
        if (teacher is null)
            return false;
        if (!teachersToStudents.TryGetValue(teacher, out HashSet<Pawn> students))
            return false;

        foreach (Pawn student in students)
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

        teachersToStudents.Remove(teacher);
        return true;
    }

    public bool RemoveStudent(Pawn student)
    {
        if (student is null)
            return false;
        if (!studentsToTeachers.TryGetValue(student, out HashSet<ResidentKnight> teachers))
            return false;

        foreach (ResidentKnight teacher in teachers)
        {
            if (teachersToStudents.TryGetValue(teacher, out HashSet<Pawn> teacherStudents))
            {
                teachers.Remove(teacher);
                if (teacherStudents.Count == 0)
                {
                    teachersToStudents.Remove(teacher);
                }
            }
        }

        studentsToTeachers.Remove(student);
        return true;
    }

    public IReadOnlyCollection<ResidentKnight> GetTeachersOfStudent(Pawn studentPawn)
    {
        if (studentsToTeachers.TryGetValue(studentPawn, out HashSet<ResidentKnight> teachers))
            return teachers;
        return [];
    }

    public IReadOnlyCollection<Pawn> GetStudentsOfTeacher(ResidentKnight teacherRecord)
    {
        if (teachersToStudents.TryGetValue(teacherRecord, out HashSet<Pawn> students))
            return students;
        return [];
    }

    public bool IsStudentOfKnight(Pawn studentPawn, ResidentKnight knightRecord)
    {
        if (studentPawn is null || knightRecord is null)
            return false;
        if (studentsToTeachers.TryGetValue(studentPawn, out HashSet<ResidentKnight> teachers))
        {
            return teachers.Contains(knightRecord);
        }
        return false;
    }

    public void TickDay()
    {

    }

    private class StudentTeacherPair : IExposable
    {
        public Pawn student;
        public ResidentKnight teacher;

        public void ExposeData()
        {
            Scribe_References.Look(ref student, nameof(student));
            Scribe_References.Look(ref teacher, nameof(teacher));
        }
    }
}
