using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public sealed class WorldObject_FieldSurvey : WorldObject_InteractWithFixedCaravanBase, ISingleBranchRelatedReferenceable
{
    public enum RegionalFeature : byte
    {
        None,

        ComplexEcosystem,
        OriginalEcology,
        DangerousEcology,

        VariableClimate,

        StandardLandform,
        SimpleLandform,
        FineLandform
    }

    public static List<RegionalFeature> RegionalFeatures = Enum.GetValues(typeof(RegionalFeature)).OfType<RegionalFeature>().ToList();

    private enum WorkType
    {
        Meteorological,
        Exploration,
        Information
    }

    private Branch branch;
    public Branch Branch => branch;
    BranchDemandType demandType;

    private RegionalFeature regionalFeatureI;
    private RegionalFeature regionalFeatureII;
    private bool featureExposed;

    private WorkType curWorkType;

    private int nextAvailableTick = -1;

    private int maxSpecialEvent;
    private int occurSpecialEvent;

    private int meteorologicalDataRequire;
    private int meteorologicalDataGained;

    private int geologicalInsights;
    private float PerInsightToInfo => HasRegionalFeature(RegionalFeature.SimpleLandform) ? 0.5f : 0.25f;

    private float infoCompleteness;
    public float InfoCompleteness
    {
        get => infoCompleteness;
        private set => infoCompleteness = Mathf.Clamp(value, 0f, 5f);
    }

    public override int TicksNeeded
    {
        get
        {
            return curWorkType switch
            {
                WorkType.Meteorological => 8 * 2500,
                WorkType.Information => HasRegionalFeature(RegionalFeature.FineLandform) ? 16 * 2500 : 6 * 2500,
                WorkType.Exploration => HasRegionalFeature(RegionalFeature.OriginalEcology) ? 12 * 2500 : 6 * 2500,
                _ => 6 * 2500,
            };
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref demandType, "demandType");

        Scribe_Values.Look(ref regionalFeatureI, "regionalFeatureI");
        Scribe_Values.Look(ref regionalFeatureII, "regionalFeatureII");
        Scribe_Values.Look(ref featureExposed, "featureExposed", defaultValue: false);

        Scribe_Values.Look(ref curWorkType, "curWorkType");

        Scribe_Values.Look(ref nextAvailableTick, "nextAvailableTick", -1);

        Scribe_Values.Look(ref maxSpecialEvent, "maxSpecialEvent", 0);
        Scribe_Values.Look(ref occurSpecialEvent, "occurSpecialEvent", 0);
        Scribe_Values.Look(ref meteorologicalDataRequire, "meteorologicalDataRequire", 0);
        Scribe_Values.Look(ref meteorologicalDataGained, "meteorologicalDataGained", 0);
        Scribe_Values.Look(ref geologicalInsights, "geologicalInsights", 0);
        Scribe_Values.Look(ref infoCompleteness, "infoCompleteness", 0f);
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine("OARO_FieldSurvey_MeteorologicalData".Translate(meteorologicalDataGained, meteorologicalDataRequire)
                                                                .Colorize(meteorologicalDataGained >= meteorologicalDataRequire ? Color.green : Color.white));
        sb.AppendInNewLine("OARO_FieldSurvey_GeologicalInsights".Translate(geologicalInsights));

        sb.AppendInNewLine("OARO_FieldSurvey_GeologicalInsightsInfo".Translate((geologicalInsights * PerInsightToInfo).ToStringPercent("F2")));

        sb.AppendInNewLine("OARO_FieldSurvey_InfoCompleteness".Translate(infoCompleteness.ToStringPercent("F2"))
                                                              .Colorize(infoCompleteness >= 5f ? Color.green : Color.white));

        if (featureExposed)
        {
            sb.AppendLine();
            GetRegionalFeatureDesc(sb);
        }

        return sb.ToString();
    }

    private void GetRegionalFeatureDesc(StringBuilder sb)
    {
        if (regionalFeatureI != RegionalFeature.None)
        {
            sb.AppendInNewLine($"OARO_FieldSurvey_{regionalFeatureI}".Translate());
            sb.Append(": ");
            sb.Append($"OARO_FieldSurvey_{regionalFeatureI}_Desc".Translate());
        }
        if (regionalFeatureII != RegionalFeature.None)
        {
            sb.AppendInNewLine($"OARO_FieldSurvey_{regionalFeatureII}".Translate());
            sb.Append(": ");
            sb.Append($"OARO_FieldSurvey_{regionalFeatureII}_Desc".Translate());
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }

        yield return new Command_Action()
        {
            defaultLabel = "DEV: Add 1 Meteorological",
            action = delegate { meteorologicalDataGained++; }
        };
        yield return new Command_Action()
        {
            defaultLabel = "DEV: Add 1 Geological",
            action = delegate
            {
                geologicalInsights++;
                InfoCompleteness += geologicalInsights * PerInsightToInfo;
            }
        };
        yield return new Command_Action()
        {
            defaultLabel = "DEV: Add 100% Information",
            action = delegate
            {
                InfoCompleteness += 1f;
            }
        };
    }

    public void InitOrderBranch(Branch branch)
    {
        this.branch = branch;
    }

    public bool HasRegionalFeature(RegionalFeature feature)
    {
        return regionalFeatureI == feature || regionalFeatureII == feature;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            branch = null;
        }
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
        }
    }

    public override void PostAdd()
    {
        base.PostAdd();
        (Branch branch, demandType) = QuestPart_BranchDemandWatcher.GetBranchDemand(quest);
        InitOrderBranch(branch);

        meteorologicalDataRequire = demandType == BranchDemandType.Urgency ? 3 : 2;

        regionalFeatureI = RegionalFeatures[Rand.Range(1, RegionalFeatures.Count)];
        regionalFeatureII = RegionalFeatures[Rand.Range(0, RegionalFeatures.Count)];

        maxSpecialEvent = HasRegionalFeature(RegionalFeature.ComplexEcosystem) ? 3 : 1;
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (Find.TickManager.TicksGame < nextAvailableTick)
        {
            int cooldownTicksLeft = nextAvailableTick - Find.TickManager.TicksGame;
            Messages.Message("WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod()), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    public override bool StartWork(Caravan caravan)
    {
        Find.WindowStack.Add(ArriveDialog(caravan));
        return true;
    }

    protected override void InterruptWork() { }

    private Dialog_NodeTreeWithFactionInfo ArriveDialog(Caravan caravan)
    {
        DiaNode arriveNode = new("OARO_FieldSurveyInfo".Translate());

        DiaOption meteorologicalOpt = new("OARO_FieldSurvey_Meteorological".Translate())
        {
            action = delegate { StartWork(WorkType.Meteorological, caravan); },
            resolveTree = true
        };
        arriveNode.options.Add(meteorologicalOpt);

        if (demandType != BranchDemandType.Supplementary)
        {
            DiaOption informationOpt = new("OARO_FieldSurvey_Information".Translate())
            {
                action = delegate { StartWork(WorkType.Information, caravan); },
                resolveTree = true
            };
            arriveNode.options.Add(informationOpt);

            DiaOption explorationOpt = new("OARO_FieldSurvey_Exploration".Translate())
            {
                action = delegate { StartWork(WorkType.Exploration, caravan); },
                resolveTree = true
            };
            arriveNode.options.Add(explorationOpt);
        }

        DiaOption ignoreOpt = new("Ignore".Translate())
        {
            resolveTree = true
        };
        arriveNode.options.Add(ignoreOpt);

        return new Dialog_NodeTreeWithFactionInfo(arriveNode, Faction);
    }

    private void StartWork(WorkType workType, Caravan caravan)
    {
        curWorkType = workType;
        base.StartWork(caravan);
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is null)
        {
            return;
        }

        switch (curWorkType)
        {
            case WorkType.Meteorological:
                {
                    meteorologicalDataGained++;
                    geologicalInsights++;
                    float gainInfo = PerInsightToInfo;
                    InfoCompleteness += gainInfo;
                    nextAvailableTick = Find.TickManager.TicksGame + GetCoolDownTicks();
                    Messages.Message("OARO_FieldSurvey_Meteorological_Finished".Translate(gainInfo.ToStringPercent("F2")), MessageTypeDefOf.PositiveEvent);
                    return;
                }
            case WorkType.Exploration:
                {
                    int gainInsights = HasRegionalFeature(RegionalFeature.StandardLandform) ? 2 : 1;
                    geologicalInsights += gainInsights;
                    float gainInfo = gainInsights * PerInsightToInfo;
                    InfoCompleteness += gainInfo;
                    Messages.Message("OARO_FieldSurvey_Exploration_Finished".Translate(gainInsights, gainInfo.ToStringPercent("F2")), MessageTypeDefOf.PositiveEvent);

                    if (occurSpecialEvent >= maxSpecialEvent)
                    {
                        return;
                    }

                    float eventChace = 0.1f + OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Intellectual) * 0.04f;
                    if (Rand.Chance(eventChace))
                    {
                        occurSpecialEvent++;
                        if (featureExposed)
                        {

                        }
                        else
                        {

                        }
                    }
                    return;
                }
            case WorkType.Information:
                {
                    int maxSkillLevel = -1;
                    int totalSkillLevel = 0;
                    foreach (Pawn p in associatedFixedCaravan.PawnsListForReading)
                    {
                        int skillLevel = p.skills?.GetSkill(SkillDefOf.Intellectual).GetLevel() ?? 0;
                        totalSkillLevel += skillLevel;
                        if (skillLevel > maxSkillLevel)
                        {
                            maxSkillLevel = skillLevel;
                        }
                    }
                    if (HasRegionalFeature(RegionalFeature.DangerousEcology) && maxSkillLevel < 15)
                    {
                        Messages.Message("OARO_FieldSurvey_Information_DangerousEcology".Translate(), MessageTypeDefOf.PositiveEvent);

                    }
                    else
                    {
                        float gainInfo = maxSkillLevel * 1.5f + totalSkillLevel * 0.5f;
                        InfoCompleteness += gainInfo;
                        Messages.Message("OARO_FieldSurvey_Information_Finished".Translate(gainInfo.ToStringPercent("F2")), MessageTypeDefOf.PositiveEvent);

                    }

                    return;
                }
        }
    }

    private int GetCoolDownTicks()
    {
        if (curWorkType == WorkType.Meteorological)
        {
            return HasRegionalFeature(RegionalFeature.VariableClimate) ? 30 * 2500 : 16 * 2500;
        }
        return 0;
    }
    public override void Destroy()
    {
        if (meteorologicalDataGained >= meteorologicalDataRequire)
        {
            QuestUtility.SendQuestTargetSignals(questTags, "meteorologicalDataResolved", this.Named("SUBJECT"));
            if (infoCompleteness >= 5f)
            {
                QuestUtility.SendQuestTargetSignals(questTags, "infoCompleted", this.Named("SUBJECT"));
            }
        }
        else
        {
            QuestUtility.SendQuestTargetSignals(questTags, "meteorologicalDataNotResolved", this.Named("SUBJECT"));
        }
        base.Destroy();
    }
}
