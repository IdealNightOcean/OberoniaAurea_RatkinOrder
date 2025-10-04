using OberoniaAurea_Frame;
using RimWorld;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 城建规划 - 建设中的城镇
/// </summary>
internal class WorldObject_TownUnderConstruction : WorldObject_CriticalBranchDemand
{
    private static readonly int[] WorkProgressBoundary = [0, 200, 400, 700, 1000, int.MaxValue];
    private static readonly float[] DesignPerfectionScaleChange = [1f, 0.6f, 0.4f, 0.2f, 0.1f, 1f];

    private static readonly string[] ResidentCliqueKeys = ["SeniorResident", "VillageResident", "RemoteResident", "FramerResident"];
    private static string RandomResidentCliqueKey => ResidentCliqueKeys[Rand.Range(0, ResidentCliqueKeys.Length)];
    private enum PolicyType : byte
    {
        Programme, //设计规划
        IntegrationLabor, //整合劳力
        InvitationSettle, //邀请定居
        Construction, //施工作业
    }
    private enum WorkType : byte
    {
        AssistConstruction, //参与工程  
        AssistInvitation //邀请定居
    }

    public override int TicksNeeded => 30000;
    protected override int PeriodicCheckInterval => 15000;

    private PolicyType curPolicy;
    private WorkType curWork;
    private int nextPopulationGrowTick = -1;

    private float population;
    public float Population
    {
        get => population;
        set => population = Mathf.Max(0f, value);
    }

    private int workProgress;
    private float ExtraProgressFactor
    {
        get
        {
            if (designPerfection < 0.5f)
            {
                return 0f;
            }
            else
            {
                return HasQuestEffectTag("HighQualityLabor") ? (2f * designPerfection) : (0.5f + designPerfection);
            }
        }
    }

    private float ProgressAbnormalRegressChance
    {
        get
        {
            if (designPerfection > 0.5f)
            {
                return 0f;
            }
            else
            {
                return HasQuestEffectTag("HighQualityLabor") ? ((0.5f - designPerfection) * 0.25f) : ((0.5f - designPerfection) * 0.5f);
            }
        }
    }

    private int ScaleBoundary(int scale)
    {
        int boundary = WorkProgressBoundary[Mathf.Clamp(scale, 0, WorkProgressBoundary.Length - 1)];
        if (boundary >= int.MaxValue)
        {
            return boundary;
        }
        if (HasQuestEffectTag("BustlingCapital"))
        {
            return (int)GenMath.RoundTo(boundary * 1.5f, 10);
        }
        return boundary;
    }

    private int constructionScale;
    public int ConstructionScale
    {
        get => constructionScale;
        private set => constructionScale = Mathf.Clamp(value, 0, WorkProgressBoundary.Length - 1);
    }

    private float designPerfection;
    public float DesignPerfection
    {
        get => designPerfection;
        private set => designPerfection = Mathf.Clamp01(value);
    }

    public override void PostAdd()
    {
        base.PostAdd();
        nextPopulationGrowTick = Find.TickManager.TicksGame + 60000;
        population = Rand.Range(300f,400f);
        if(HasQuestEffectTag("HomelessTravelRatkin"))
        {
            population += 150f;
        }
        designPerfection = 0.5f;
        if (HasQuestEffectTag("PoorDesign"))
        {
            designPerfection -= 0.2f;
        }
    }

    protected override void PeriodicCheck()
    {
        
        if (Find.TickManager.TicksGame > nextPopulationGrowTick)
        {
            //人口变化
            nextPopulationGrowTick = Find.TickManager.TicksGame + 60000;
            float poputionGrow = constructionScale switch
            {
                0 => Rand.Range(4f, 10f),
                1 => Population += Rand.Range(14f, 36f),
                2 => Population += Rand.Range(48f, 102f),
                3 => Population += Rand.Range(140f, 240f),
                _ => Population += Rand.Range(225f, 400f)
            };
            if (CliquesManager.IsCliqueActive("TravelRatkin"))
            {
                poputionGrow *= 1.2f;
            }
            if (HasQuestEffectTag("BustlingCapital"))
            {
                poputionGrow *= 2f;
            }
            Population += poputionGrow;
            Messages.Message("OARO_TownUnderConstruction_DailyPopulationGrow".Translate(poputionGrow.ToString("F0")), MessageTypeDefOf.PositiveEvent);
            
            //设计完善度变化
            if(cliquesManager.IsCliqueActive("Engineers"))
            {
                DesignPerfection += 0.02f;
            }

        }

        //年轻居民
        int youngWorkProgress = Mathf.RoundToInt(CliquesManager.GetCliquePotency("YoungResident"));
        (youngWorkProgress, bool youngWorkAbnormalRegress) = GetWorkProgressChangeUsed(youngWorkProgress);
        AdjuestWorkProgress(youngWorkProgress, youngWorkAbnormalRegress);
        if (Rand.Chance(0.25f) || HasQuestEffectTag("RapidConstruction"))
        {
            DesignPerfection -= 0.02f;
            Messages.Message(text: "OARO_TownUnderConstruction_YoungWork_N".Translate(youngWorkProgress.ToStringWithSign(), 0.02f.ToStringPercent("F2")),
                             def: youngWorkProgress > 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NeutralEvent);
        }
        else
        {
            Messages.Message(text: "OARO_TownUnderConstruction_YoungWork_P".Translate(youngWorkProgress.ToStringWithSign()),
                             def: youngWorkProgress > 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.PositiveEvent);
        }

        //方针
        switch (curPolicy)
        {
            case PolicyType.Programme:
                {
                    float designGain = 0.01f + TotalPotency * 0.015f;
                    DesignPerfection += designGain;
                    Messages.Message(text: "OARO_TownUnderConstruction_ProgrammeResult".Translate(designGain.ToStringPercent("F2")),
                                     def: MessageTypeDefOf.PositiveEvent);
                    break;
                }
            case PolicyType.IntegrationLabor:
                {
                    CliquesManager.AdjustCliquePotency("YoungResident", 0.05f);
                    Messages.Message(text: "OARO_TownUnderConstruction_IntegrationLaborResult".Translate(0.05f.ToStringPercent("F2")),
                                     def: MessageTypeDefOf.PositiveEvent);
                    break;
                }
            case PolicyType.InvitationSettle:
                {
                    int populationGain = Rand.RangeInclusive(3, 10);
                    population += populationGain;

                    string cliqueKey = RandomResidentCliqueKey;
                    CliquesManager.AdjustCliqueWillingness(cliqueKey, 0.05f);
                    Messages.Message(text: "OARO_TownUnderConstruction_InvitationSettleResult".Translate(populationGain, CliquesManager.GetCliqueName(cliqueKey), 0.05f.ToStringPercent("F2")),
                                     def: MessageTypeDefOf.PositiveEvent);
                    break;
                }
            case PolicyType.Construction:
                {
                    int workProgressChange = Mathf.RoundToInt(5 + TotalPotency * 15);
                    (workProgressChange, bool abnormalRegress) = GetWorkProgressChangeUsed(workProgressChange);
                    AdjuestWorkProgress(workProgressChange, abnormalRegress);
                    if (workProgressChange > 0f)
                    {
                        Messages.Message(text: "OARO_TownUnderConstruction_ConstructionResult_P".Translate(workProgressChange),
                                         def: MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message(text: "OARO_TownUnderConstruction_ConstructionResult_N".Translate(-workProgressChange),
                                         def: MessageTypeDefOf.NegativeEvent);
                    }
                    break;
                }
        }
    }

    protected override void FinishWork()
    {
        switch (curWork)
        {
            case WorkType.AssistInvitation:
                {
                    int populationGain = Mathf.CeilToInt(OARO_PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Social) * 0.5f);
                    population += populationGain;

                    string cliqueKey = RandomResidentCliqueKey;
                    CliquesManager.AdjustCliqueWillingness(cliqueKey, 0.05f);

                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_TownUnderConstruction_AssistInvitationResult".Translate(populationGain, CliquesManager.GetCliqueName(cliqueKey), 0.05f.ToStringPercent("F2"))));
                    break;
                }
            case WorkType.AssistConstruction:
                {
                    int workProgressGain = associatedFixedCaravan.PawnsCount + Mathf.CeilToInt(OARO_PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Construction) * 0.25f);
                    (workProgressGain, bool abnormalRegress) = GetWorkProgressChangeUsed(workProgressGain);
                    AdjuestWorkProgress(workProgressGain, abnormalRegress);
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_TownUnderConstruction_AssistConstructionResult".Translate(workProgressGain)));
                    break;
                }
            default: break;
        }
    }

    protected override void InterruptWork() { }

    /// <summary>
    /// 工程进度只能主动增加，倒退是设计触发的
    /// </summary>
    /// <param name="originalChange">原始变化（应>0）</param>
    private (int change, bool abnormalRegress) GetWorkProgressChangeUsed(int originalChange)
    {
        if (originalChange <= 0)
        {
            return (0, false);
        }
        if (HasQuestEffectTag("RapidConstruction"))
        {
            originalChange = Mathf.FloorToInt(originalChange * 1.5f);
        }
        if (designPerfection > 0.5f)
        {
            return (Mathf.RoundToInt(originalChange * ExtraProgressFactor), false);
        }
        else
        {
            if (Rand.Chance(ProgressAbnormalRegressChance))
            {
                return (-3 * Mathf.Abs(originalChange), true);
            }
            else
            {
                return (originalChange, false);
            }
        }
    }

    private void AdjuestWorkProgress(int change, bool abnormalRegress)
    {
        if (change == 0)
        {
            return;
        }

        workProgress = Mathf.Max(0, workProgress + change);

        if (abnormalRegress)
        {
            Find.LetterStack.ReceiveLetter(
                label: "OARO_TownUnderConstruction_AbnormalRegressLabel".Translate(),
                text: "OARO_TownUnderConstruction_AbnormalRegressText".Translate(-change),
                textLetterDef: LetterDefOf.NegativeEvent,
                lookTargets: this,
                relatedFaction: Faction,
                quest: quest);
        }

        if (workProgress >= ScaleBoundary(constructionScale + 1))
        {
            ConstructionScale++;

            TaggedString taggedString = $"OARO_TownUnderConstruction_NewScaleText_{constructionScale}".Translate();

            if(!HasQuestEffectTag("GatheringEngineers"))
            {
                float designPerfectionFactor = DesignPerfectionScaleChange[constructionScale];
                designPerfection *= designPerfectionFactor;
                taggedString += "\n\n";
                taggedString += "OARO_TownUnderConstruction_NewScaleDesignChange".Translate(designPerfectionFactor.ToStringPercent("F2"), designPerfection.ToStringPercent("F2"));
            }

            Find.LetterStack.ReceiveLetter(
                label: "OARO_TownUnderConstruction_NewScaleLabel".Translate(),
                text: taggedString,
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: this,
                relatedFaction: Faction,
                quest: quest);
        }
    }
}