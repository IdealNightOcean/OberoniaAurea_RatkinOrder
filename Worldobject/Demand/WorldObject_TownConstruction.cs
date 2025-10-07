using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Utility;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 城建规划 - 建设中的城镇
/// </summary>
public sealed class WorldObject_TownConstruction : WorldObject_CriticalBranchDemand
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
        AssistInvitation //协助邀请
    }

    public override int TicksNeeded => 30000;
    protected override int PeriodicCheckInterval => 15000;

    private PolicyType curPolicy;
    private WorkType curWork;
    private int nextPopulationGrowTick = -1;

    private SiteTrader innerTrader;

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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref curPolicy, "curPolicy");
        Scribe_Values.Look(ref curWork, "curWork");
        Scribe_Values.Look(ref nextPopulationGrowTick, "nextPopulationGrowTick", -1);
        Scribe_Deep.Look(ref innerTrader, "innerTrader");

        Scribe_Values.Look(ref population, "population", 0f);
        Scribe_Values.Look(ref workProgress, "workProgress", 0);
        Scribe_Values.Look(ref constructionScale, "constructionScale", 0);
        Scribe_Values.Look(ref designPerfection, "designPerfection", 0f);
    }

    public void InitInnerTrader()
    {
        innerTrader ??= new(traderKind: OARO_ModDefOf.OARO_TownConstruction_Trader, worldObject: this, faction: Faction, refreshInterval: 10 * 60000);
        innerTrader.GenerateThings(Tile);
    }

    public override void PostAdd()
    {
        base.PostAdd();
        nextPopulationGrowTick = Find.TickManager.TicksGame + 60000;
        population = Rand.Range(300f, 400f);
        if (HasQuestEffectTag("HomelessTravelRatkin"))
        {
            population += 150f;
        }
        designPerfection = 0.5f;
        if (HasQuestEffectTag("PoorDesign"))
        {
            designPerfection -= 0.2f;
        }

        if (branch?.BuildingHandler.HasBuilding(BranchBuildingDefOf.OARO_ArchitectOffice) ?? false)
        {
            QuestClique architectClique = new("Architects")
            {
                Name = "OARO_CliqueName_Architects".Translate(),
                ActiveDesc = "OARO_CliqueActiveDesc_Architects".Translate(),
                IsActivatable = true,
            };
            CliquesManager.TryAddClique(architectClique, defaultActive: true);
        }
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine("OARO_WorldObejct_CurPolicy".Translate());
        sb.Append(": ");
        sb.Append($"OARO_TownUnderConstruction_{curPolicy}".Translate());
        if (isWorking)
        {
            sb.AppendInNewLine("OARO_WorldObejct_CurWork".Translate());
            sb.Append(": ");
            sb.Append($"OARO_TownUnderConstruction_{curWork}".Translate());
        }

        sb.AppendInNewLine("OARO_WorldObejct_Population".Translate());
        sb.Append(": ");
        sb.Append(population.ToString("F0"));

        sb.AppendInNewLine($"OARO_TownUnderConstruction_ConstructionScale".Translate());
        sb.Append(": ");
        sb.Append($"OARO_TownUnderConstruction_ConstructionScale_{constructionScale}".Translate());

        int nextProgressBoundary = ScaleBoundary(constructionScale + 1);
        if (nextProgressBoundary >= int.MaxValue)
        {
            sb.AppendInNewLine($"OARO_TownUnderConstruction_WorkProgress_Max".Translate(workProgress));
        }
        else
        {
            sb.AppendInNewLine($"OARO_TownUnderConstruction_WorkProgress".Translate(workProgress, nextProgressBoundary));
        }


        sb.AppendInNewLine($"OARO_TownUnderConstruction_DesignPerfection".Translate(designPerfection.ToStringPercent("F2")));
        if (designPerfection > 0.5f)
        {
            sb.AppendInNewLine($"OARO_TownUnderConstruction_DesignPerfection_Buff".Translate(ExtraProgressFactor.ToStringPercent("F2")).Colorize(Color.green));
        }
        else
        {
            sb.AppendInNewLine($"OARO_TownUnderConstruction_DesignPerfection_Debuff".Translate(ProgressAbnormalRegressChance.ToStringPercent("F2")).Colorize(ColorLibrary.RedReadable));
        }

        return sb.ToString();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        Command_Action command_Policy = new()
        {
            defaultLabel = "OARO_ChangePolicy".Translate(),
            defaultDesc = "OARO_ChangePolicyDesc".Translate(),
            action = () => Find.WindowStack.Add(new Dialog_NodeTree(PolicyChangeNode()))
        };
        yield return command_Policy;

        //调试按钮
        if (DebugSettings.ShowDevGizmos)
        {
            yield return new Command_Action()
            {
                defaultLabel = "DEV: +100 Population",
                action = () => Population += 500f
            };
            yield return new Command_Action()
            {
                defaultLabel = "DEV: +100 WorkProgress",
                action = () => AdjuestWorkProgress(100, abnormalRegress: false)
            };
            yield return new Command_Action()
            {
                defaultLabel = "DEV: -100 WorkProgress",
                action = () => AdjuestWorkProgress(-100, abnormalRegress: false)
            };
            yield return new Command_Action()
            {
                defaultLabel = "DEV: -100 WorkProgress (AbnormalRegress)",
                action = () => AdjuestWorkProgress(-100, abnormalRegress: true)
            };
            yield return new Command_Action()
            {
                defaultLabel = "DEV:+10% DesignPerfection",
                action = () => DesignPerfection += 0.1f
            };
            yield return new Command_Action()
            {
                defaultLabel = "DEV:-10% DesignPerfection",
                action = () => DesignPerfection -= 0.1f
            };
        }
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        Find.WindowStack.Add(new Dialog_NodeTree(ArrivedDiaNode(caravan)));
    }

    private DiaNode ArrivedDiaNode(Caravan caravan)
    {
        DiaNode rootNode = new("OARO_TownUnderConstruction_ArrivalInfo".Translate());
        if (!isWorking)
        {
            foreach (WorkType workType in EnumUtility.GetValues<WorkType>())
            {
                DiaOption workOpt = new($"OARO_TownUnderConstruction_{workType}".Translate())
                {
                    action = delegate
                    {
                        curWork = workType;
                        base.Notify_CaravanArrived(caravan);
                    },
                    resolveTree = true
                };
                rootNode.options.Add(workOpt);
            }
        }

        DiaOption deliveryOpt = new("OARO_TownUnderConstruction_Delivery".Translate())
        {
            action = () => DeliveryResult(caravan),
            resolveTree = true
        };
        if (!CaravanInventoryUtility.HasThings(caravan, OARO_ThingDefOf.OARO_DesignDrawing, 1))
        {
            deliveryOpt.Disable("OAFrame_NeedCountOfThing".Translate(OARO_ThingDefOf.OARO_DesignDrawing.label, 1));
        }
        rootNode.options.Add(deliveryOpt);

        DiaOption supplyOpt = new("OARO_TownUnderConstruction_Supply".Translate())
        {
            action = () => SupplyResult(caravan),
            resolveTree = true
        };
        if (!caravan.HasEnoughThingsOfCategory(ThingCategoryDefOf.StoneBlocks, 10))
        {
            supplyOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingCategoryDefOf.StoneBlocks.label, 10));
        }
        rootNode.options.Add(supplyOpt);

        DiaOption buyOpt = new("OARO_TownUnderConstruction_Buy".Translate())
        {
            resolveTree = true
        };
        if (innerTrader is null)
        {
            buyOpt.Disable("OARO_TownUnderConstruction_BuildersInactive".Translate());
        }
        else
        {
            buyOpt.action = delegate
            {
                Pawn pawn = BestCaravanPawnUtility.FindBestNegotiator(caravan);
                if (pawn is null)
                {
                    Messages.Message("OAFrame_MessageNoTrader".Translate(), caravan, MessageTypeDefOf.NegativeEvent, historical: false);
                    return;
                }
                Find.WindowStack.Add(new Dialog_Trade(pawn, innerTrader));
            };
        }
        ;
        rootNode.options.Add(buyOpt);

        rootNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);
        return rootNode;
    }

    private DiaNode PolicyChangeNode()
    {
        DiaNode rootNode = new("OARO_TownUnderConstruction_PolicyInfo".Translate());

        foreach (PolicyType policyType in EnumUtility.GetValues<PolicyType>())
        {
            DiaOption policyOpt = new($"OARO_TownUnderConstruction_{policyType}".Translate())
            {
                action = () => { curPolicy = policyType; },
                resolveTree = true
            };
            rootNode.options.Add(policyOpt);
        }
        return rootNode;
    }

    protected override void TickInterval(int delta)
    {
        innerTrader?.TickInterval(delta);
        base.TickInterval(delta);
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

            //设计完善度变化
            float designPerfectionChange = 0f;
            if (cliquesManager.IsCliqueActive("Architects"))
            {
                designPerfectionChange += 0.02f;
            }
            if (cliquesManager.IsCliqueActive("Engineers"))
            {
                designPerfectionChange += 0.02f;
            }
            if (HasQuestEffectTag("DesignerNightmare"))
            {
                designPerfectionChange *= 0.5f;
            }
            DesignPerfection += designPerfectionChange;

            Messages.Message(text: "OARO_TownUnderConstruction_DailyReport".Translate(poputionGrow.ToString("F0"), designPerfectionChange.ToStringPercent("F2")),
                             def: MessageTypeDefOf.PositiveEvent);
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

    private void DeliveryResult(Caravan caravan)
    {
        int giveCount = OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, OARO_ThingDefOf.OARO_DesignDrawing, int.MaxValue);
        if (giveCount > 0)
        {
            float designPerfectionGain = giveCount * 0.04f;
            DesignPerfection += designPerfectionGain;
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_TownUnderConstruction_DeliveryResult".Translate(giveCount, OARO_ThingDefOf.OARO_DesignDrawing.label, designPerfectionGain.ToStringPercent("F2"))));
        }
    }

    private void SupplyResult(Caravan caravan)
    {
        int totalTakeCount = 0;
        List<Thing> takeStones = CaravanInventoryUtility.TakeThings(caravan, TakeStoneCount);
        foreach (Thing stone in takeStones)
        {
            stone.Destroy();
        }
        (int workProgressGain, _) = GetWorkProgressChangeUsed(Mathf.RoundToInt(totalTakeCount * 0.1f), canRegress: false);
        AdjuestWorkProgress(workProgressGain, abnormalRegress: false);
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_TownUnderConstruction_SupplyResult".Translate(totalTakeCount, workProgressGain)));

        int TakeStoneCount(Thing t)
        {
            if (t.HasThingCategory(ThingCategoryDefOf.StoneBlocks))
            {
                totalTakeCount += t.stackCount;
                return t.stackCount;
            }
            return 0;
        }
    }

    /// <summary>
    /// 工程进度只能主动增加，倒退是设计触发的
    /// </summary>
    /// <param name="originalChange">原始变化（应>0）</param>
    private (int change, bool abnormalRegress) GetWorkProgressChangeUsed(int originalChange, bool canRegress = true)
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
            if (canRegress && Rand.Chance(ProgressAbnormalRegressChance))
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

            if (!HasQuestEffectTag("GatheringEngineers"))
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

    public override void Destroy()
    {
        innerTrader?.Destory();
        QuestUtility.SendQuestTargetSignals(questTags, "PopulationSettled", this.Named("SUBJECT"), population.Named("POPULATION"));
        base.Destroy();
    }
}