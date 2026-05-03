using OberoniaAurea_Frame;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 辖区执勤数据
/// </summary>
public class JurisdictionDutyData : IExposable
{
    private static readonly int[] AssistanceRequestThresholds = [100, 200, 400, 600, 1000, 1400];

    private static readonly AssistanceRequestWorker[] Workers =
    [
        new AssistanceRequestWorker_BasicWork(),
        new AssistanceRequestWorker_SkillRequired(),
        new AssistanceRequestWorker_AttributeRequired(),
        new AssistanceRequestWorker_VirtueRequired(),
        new AssistanceRequestWorker_AcademicRequired()
    ];

    private int curProgress;
    public int CurProgress => curProgress;

    private int progressCeiling;
    public int ProgressCeiling => progressCeiling;

    private float dailyProgress;
    public float DailyProgress => dailyProgress;

    private List<CompletedObjective> completedObjectives = [];
    public IReadOnlyList<CompletedObjective> CompletedObjectives => completedObjectives;

    private float dutyRisk;
    public float DutyRisk => dutyRisk;

    private List<KnightAcademicDef> dutyAcademics = [];
    public IReadOnlyList<KnightAcademicDef> DutyAcademics => dutyAcademics;

    private List<AssistanceRequest> assistanceRequests = [];
    public IReadOnlyList<AssistanceRequest> AssistanceRequests => assistanceRequests;

    public float ProgressRatio => progressCeiling > 0 ? (float)curProgress / progressCeiling : 0f;

    private int nextObjectiveCheckProgress = 50;
    private int nextRequestThresholdIndex;

    private int hourCounter;

    public JurisdictionDutyData() { }
    public JurisdictionDutyData(Branch branch) { Initialize(branch); }

    public void Initialize(Branch branch)
    {
        float postureMultiplier = branch.TaskHandler.CurRadicalismDegree switch
        {
            BranchTaskHandler.RadicalismDegree.StabilityFocused => 0.75f,
            BranchTaskHandler.RadicalismDegree.Aggressive => 1.5f,
            _ => 1f
        };

        int baseCeiling = 200 + branch.PopulationHandler.Population / 5;
        progressCeiling = Mathf.RoundToInt(baseCeiling * postureMultiplier);

        dailyProgress = CalculateDailyProgress(branch);

        dutyRisk = CalculateDutyRisk(branch);

        GenerateDutyAcademics(branch);
        nextObjectiveCheckProgress = 50;
        nextRequestThresholdIndex = 0;
        hourCounter = 0;
    }

    public void TickHour(Branch branch)
    {
        hourCounter++;
        if (hourCounter >= 2)
        {
            hourCounter = 0;
            float increment = dailyProgress / 12f;
            AddProgress(increment, branch);
        }
    }

    public void AddProgress(float amount, Branch branch)
    {
        if (progressCeiling <= 0) return;

        curProgress += Mathf.RoundToInt(amount);
        if (curProgress >= progressCeiling)
        {
            curProgress = progressCeiling;
        }

        CheckObjectiveGeneration(branch);
        CheckAssistanceRequestGeneration(branch);
    }

    public void CheckObjectiveGeneration(Branch branch)
    {
        while (curProgress >= nextObjectiveCheckProgress)
        {
            nextObjectiveCheckProgress += 50;
            float passRate = Mathf.Clamp01(1f - completedObjectives.Count * 0.05f);
            if (Rand.Chance(passRate))
            {
                BranchMedalDef medal = PickMedalDef(branch);
                completedObjectives.Add(new CompletedObjective(CompletedObjective.ObjectiveType.Normal, medal));
            }

            if (Rand.Chance(dutyRisk))
            {
                int memberLoss = Rand.RangeInclusive(1, 4);
                branch.Squad.AdjustCrew(member: -memberLoss, commander: 0f);
            }
        }
    }

    public void CheckAssistanceRequestGeneration(Branch branch)
    {
        while (nextRequestThresholdIndex < AssistanceRequestThresholds.Length
            && curProgress >= AssistanceRequestThresholds[nextRequestThresholdIndex]
            && assistanceRequests.Count < 6)
        {
            GenerateAssistanceRequest(branch);
            nextRequestThresholdIndex++;
        }
    }

    public void OnWorkCycleCompleted(FixedCaravan fixedCaravan)
    {
        List<AssistanceRequest> activeRequests = [];
        for (int i = 0; i < assistanceRequests.Count; i++)
        {
            if (assistanceRequests[i].Participating && !assistanceRequests[i].Completed)
            {
                activeRequests.Add(assistanceRequests[i]);
            }
        }

        if (activeRequests.Count <= 0) return;

        int activeCount = activeRequests.Count;
        for (int i = 0; i < activeRequests.Count; i++)
        {
            AssistanceRequest request = activeRequests[i];
            AssistanceRequestWorker worker = GetWorker(request.Type);
            float progress = worker.CalculateDailyProgress(fixedCaravan, request) / activeCount;
            request.AddProgress(progress);
            if (request.Completed)
            {
                OnAssistanceRequestCompleted(request);
            }
        }
    }

    private void OnAssistanceRequestCompleted(AssistanceRequest request)
    {
        int medalCount = Mathf.CeilToInt(request.ProgressCeiling / 100f) + 1;
        BranchMedalDef medal = PickMedalDef(null);
        completedObjectives.Add(new CompletedObjective(CompletedObjective.ObjectiveType.Assistance, medal, medalCount));
    }

    private void GenerateAssistanceRequest(Branch branch)
    {
        AssistanceRequest.RequestType type = (AssistanceRequest.RequestType)Rand.RangeInclusive(0, 4);
        AssistanceRequestWorker worker = GetWorker(type);
        AssistanceRequest request = new();
        worker.Initialize(request, dutyAcademics);

        int rawCeiling = Mathf.RoundToInt(curProgress * Rand.Range(0.25f, 0.50f));
        int adjustedCeiling = Mathf.Min(rawCeiling, 300) + Mathf.Max(0, rawCeiling - 300) / 2;
        int finalCeiling = Mathf.Clamp(adjustedCeiling, 100, 500);

        float traditionValue = GetTraditionValueForTaskType(branch);
        float baseDaily = 20f + 0.3f * branch.Squad.AllCrewCount + traditionValue * 30f;
        float requestDaily = baseDaily * 0.3f;

        request.Initialize(
            type: type,
            title: request.Title,
            reqDesc: worker.GenerateRequirementDesc(request),
            ceiling: finalCeiling,
            daily: requestDaily,
            academic: request.RelatedAcademic,
            skill: request.RelatedSkill,
            skillLvl: request.SkillLevelRequired,
            stat: request.RelatedStat,
            statVal: request.StatValueRequired,
            virtue: request.RelatedVirtue
        );

        assistanceRequests.Add(request);
    }

    private void GenerateDutyAcademics(Branch branch)
    {
        dutyAcademics.Clear();
        BranchTaskType taskType = branch.TaskHandler.FocusedTaskType;
        KnightChivalryDef chivalry = GetChivalryForTaskType(taskType);

        if (chivalry is not null)
        {
            List<KnightAcademicDef> allAcademicDefs = DefDatabase<KnightAcademicDef>.AllDefsListForReading;
            KnightAcademicDef generalAcademic = null;
            KnightAcademicDef honorOrTraditional = null;


            for (int i = 0; i < allAcademicDefs.Count; i++)
            {
                KnightAcademicDef a = allAcademicDefs[i];
                if (a.chivalry == chivalry)
                {
                    if (a.academicType == KnightAcademicDef.AcademicType.Geneal && generalAcademic is null)
                    {
                        generalAcademic = a;
                    }
                    else if (a.academicType != KnightAcademicDef.AcademicType.Geneal && honorOrTraditional is null)
                    {
                        honorOrTraditional = a;
                    }
                }
            }
            if (generalAcademic is not null)
            {
                dutyAcademics.Add(generalAcademic);
            }

            if (honorOrTraditional is not null)
            {
                dutyAcademics.Add(honorOrTraditional);
            }
        }

        KnightAcademicDef randomAcademic = null;
        List<KnightAcademicDef> allAcademics = DefDatabase<KnightAcademicDef>.AllDefsListForReading;
        List<KnightAcademicDef> nonGeneral = [];
        for (int i = 0; i < allAcademics.Count; i++)
        {
            if (allAcademics[i].academicType != KnightAcademicDef.AcademicType.Geneal)
            {
                nonGeneral.Add(allAcademics[i]);
            }
        }
        if (nonGeneral.Count > 0)
        {
            randomAcademic = nonGeneral[Rand.Range(0, nonGeneral.Count)];
        }
        if (randomAcademic is not null)
        {
            dutyAcademics.Add(randomAcademic);
        }
    }

    private static KnightChivalryDef GetChivalryForTaskType(BranchTaskType taskType)
    {
        return taskType switch
        {
            BranchTaskType.CrimeFighting => BranchMedalDefOf.OARO_Courage?.chivalry,
            BranchTaskType.StabilityMaintenance => BranchMedalDefOf.OARO_Rescue?.chivalry,
            BranchTaskType.Assistance => BranchMedalDefOf.OARO_Tenacity?.chivalry,
            BranchTaskType.Supervision => BranchMedalDefOf.OARO_Justice?.chivalry,
            _ => null
        };
    }

    private static float GetTraditionValueForTaskType(Branch branch)
    {
        KnightChivalryDef chivalry = GetChivalryForTaskType(branch.TaskHandler.FocusedTaskType);
        if (chivalry is null) return 0f;

        int count = 0;
        IReadOnlyList<BranchTradition> traditions = branch.TraditionHandler.Traditions;
        for (int i = 0; i < traditions.Count; i++)
        {
            if (traditions[i].Def?.Chivalry == chivalry)
            {
                count++;
            }
        }
        return count;
    }

    private static float CalculateDailyProgress(Branch branch)
    {
        float postureDailyMultiplier = branch.TaskHandler.CurRadicalismDegree switch
        {
            BranchTaskHandler.RadicalismDegree.StabilityFocused => 0.75f,
            BranchTaskHandler.RadicalismDegree.Aggressive => 1.25f,
            _ => 1f
        };

        float traditionValue = GetTraditionValueForTaskType(branch);
        float baseDaily = 20f + 0.3f * branch.Potency + traditionValue * 30f;
        return baseDaily * postureDailyMultiplier;
    }

    private static float CalculateDutyRisk(Branch branch)
    {
        BranchTaskHandler.RadicalismDegree degree = branch.TaskHandler.CurRadicalismDegree;
        float baseRisk = degree switch
        {
            BranchTaskHandler.RadicalismDegree.StabilityFocused => 0.05f,
            BranchTaskHandler.RadicalismDegree.Aggressive => 0.20f,
            _ => 0.10f
        };
        float publicSecurity = branch.PopulationHandler.PublicSecurity;
        return baseRisk * (1f + Mathf.Max(0f, 1f - publicSecurity));
    }

    private static BranchMedalDef PickMedalDef(Branch branch)
    {
        BranchTaskType focusedType = branch?.TaskHandler.FocusedTaskType ?? BranchTaskType.General;

        List<(BranchTaskType taskType, float weight)> taskTypeWeighters = new(BranchTaskTypeExtension.EnumArr.Length);
        foreach (BranchTaskType taskType in BranchTaskTypeExtension.EnumArr)
        {
            if (taskType != focusedType)
            {
                taskTypeWeighters.Add((taskType, 10f));
            }
        }
        taskTypeWeighters.Add((focusedType, taskTypeWeighters.Count * 10f));

        BranchTaskType rewardTaskType = taskTypeWeighters.RandomElementByWeight(pair => pair.weight).taskType;

        List<BranchMedalDef> rewardMedalDefs = BranchTaskTypeExtension.GetMedalDefsByTaskType(rewardTaskType);
        if (!rewardMedalDefs.NullOrEmpty())
        {
            return rewardMedalDefs.RandomElement();
        }
        else
        {
            return DefDatabase<BranchMedalDef>.GetRandom();
        }
    }

    private static AssistanceRequestWorker GetWorker(AssistanceRequest.RequestType type)
    {
        int index = (int)type;
        if (index >= 0 && index < Workers.Length)
        {
            return Workers[index];
        }
        return Workers[0];
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref curProgress, nameof(curProgress), 0);
        Scribe_Values.Look(ref progressCeiling, nameof(progressCeiling), 0);
        Scribe_Values.Look(ref dailyProgress, nameof(dailyProgress), 0f);
        Scribe_Collections.Look(ref completedObjectives, nameof(completedObjectives), LookMode.Deep);
        Scribe_Values.Look(ref dutyRisk, nameof(dutyRisk), 0f);
        Scribe_Collections.Look(ref dutyAcademics, nameof(dutyAcademics), LookMode.Def);
        Scribe_Collections.Look(ref assistanceRequests, nameof(assistanceRequests), LookMode.Deep);
        Scribe_Values.Look(ref nextObjectiveCheckProgress, nameof(nextObjectiveCheckProgress), 50);
        Scribe_Values.Look(ref nextRequestThresholdIndex, nameof(nextRequestThresholdIndex), 0);
        Scribe_Values.Look(ref hourCounter, nameof(hourCounter), 0);
    }
}
