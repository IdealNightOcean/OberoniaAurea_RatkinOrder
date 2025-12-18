using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 饥荒村庄（特化类）
/// </summary>
public sealed class WorldObject_FamineVillage : WorldObject_InteractWithFixedCaravan_Nameable, IThingRequester, ISingleBranchRelated
{
    private enum WorkType : byte
    {
        Direct,
        GainTrust,
        PryInfo,
        Precise,
        Feast
    }

    private int ticksNeeded = 30000;
    public override int TicksNeeded => ticksNeeded;
    private WorkType curWorkType;

    private ThingDef requestDef;
    private int requestCount = -1;
    private int requestCountLeft = -1;
    public bool IsRequestActive => requestCountLeft > 0 && requestDef is not null;

    private int fulfillCount;

    private Branch branch;
    public Branch Branch => branch;
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;

    private float curTrust;
    private int gainTrustCount;
    private int validInfoCount;

    private bool HasFeastLater { get; set; }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksNeeded, "ticksNeeded", 0);
        Scribe_Values.Look(ref curWorkType, "curWorkType");

        Scribe_Defs.Look(ref requestDef, "requestDef");
        Scribe_Values.Look(ref requestCount, "requestCount", -1);
        Scribe_Values.Look(ref requestCountLeft, "requestCountLeft", -1);

        Scribe_Values.Look(ref fulfillCount, "fulfillCount", 0);

        Scribe_References.Look(ref branch, "branch");

        Scribe_Values.Look(ref curTrust, "curTrust", 0f);
        Scribe_Values.Look(ref gainTrustCount, "gainTrustCount", 0);
        Scribe_Values.Look(ref validInfoCount, "validInfoCount", 0);
    }

    public override void PostAdd()
    {
        base.PostAdd();
        (Branch branch, BranchDemand.DemandType demandType) = QuestPart_BranchDemandWatcher.GetBranchDemand(quest);
        SetOrderBranch(branch);
        if (demandType == BranchDemand.DemandType.Urgency)
        {
            curTrust = Rand.Range(0.3f, 0.45f);
        }
        else
        {
            curTrust = Rand.Range(0.05f, 0.15f);
        }
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine("OARO_FamineVillage_CurTrust".Translate(curTrust.ToString("0.##")));
        return sb.ToString();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        if (DebugSettings.ShowDevGizmos)
        {
            Command_Action command_GainTrust = new()
            {
                defaultLabel = "DEV: +10% Trust",
                action = () => curTrust = Mathf.Clamp01(curTrust + 0.1f)
            };
            yield return command_GainTrust;
        }
    }

    public void SetOrderBranch(Branch branch)
    {
        this.branch = branch;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (RatkinOrder == ratkinOrder)
        {
            branch = null;
        }
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            branch = null;
        }
    }

    public void InitThingRequest(ThingDef requestDef, int requestCount)
    {
        this.requestDef = requestDef;
        this.requestCount = requestCount;
        requestCountLeft = requestCount;
    }

    public void DisableRequest()
    {
        requestDef = null;
        requestCount = -1;
        requestCountLeft = -1;
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        Find.WindowStack.Add(Dialog_StartWork(caravan));
    }

    private Dialog_NodeTreeWithFactionInfo Dialog_StartWork(Caravan caravan)
    {
        TaggedString nodeText = fulfillCount > 0 ? "OARO_FamineVillage_Star".Translate() : "OARO_FamineVillage_FirstStar".Translate();
        DiaNode rootNode = new(nodeText);

        DiaOption directOpt = new("OARO_FamineVillage_Direct".Translate())
        {
            action = delegate
            {
                curWorkType = WorkType.Direct;
                FulfillRequest(caravan);
            },
            resolveTree = true
        };
        rootNode.options.Add(directOpt);

        DiaOption gainTrustOpt = new("OARO_FamineVillage_GainTrust".Translate())
        {
            action = delegate { StartWork(WorkType.GainTrust, caravan); },
            resolveTree = true
        };
        rootNode.options.Add(gainTrustOpt);

        DiaOption pryInfoOpt = new("OARO_FamineVillage_PryInfo".Translate())
        {
            action = delegate { StartWork(WorkType.PryInfo, caravan); },
            resolveTree = true
        };
        rootNode.options.Add(pryInfoOpt);

        DiaOption preciseOpt = new("OARO_FamineVillage_Precise".Translate())
        {
            action = delegate { StartWork(WorkType.Precise, caravan); },
            resolveTree = true
        };
        if (validInfoCount <= 0)
        {
            preciseOpt.Disable("OARO_FamineVillage_InsufficientInfo".Translate());
        }
        rootNode.options.Add(preciseOpt);

        DiaOption waitOpt = new("Wait".Translate())
        {
            resolveTree = true
        };

        rootNode.options.Add(waitOpt);

        return new Dialog_NodeTreeWithFactionInfo(rootNode, Faction);
    }

    private void StartWork(WorkType workType, Caravan caravan)
    {
        curWorkType = workType;
        switch (curWorkType)
        {
            case WorkType.GainTrust:
                ticksNeeded = 6 * 2500;
                break;
            case WorkType.PryInfo:
                ticksNeeded = 8 * 2500;
                break;
            case WorkType.Precise:
                ticksNeeded = 4 * 2500;
                break;
            case WorkType.Feast:
                ticksNeeded = 2 * 2500;
                break;
            default: return;
        }

        base.Notify_CaravanArrived(caravan);
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is null)
        {
            return;
        }
        switch (curWorkType)
        {
            case WorkType.GainTrust:
                {
                    float gainedTrust;
                    if (gainTrustCount <= 0)
                    {
                        gainedTrust = (RatkinOrder?.Esteem ?? 30) * 0.01f;
                    }
                    else
                    {
                        float maxStat = OAFrame_PawnUtility.GetMaxStatOfPawns(associatedFixedCaravan.PawnsListForReading, StatDefOf.NegotiationAbility);
                        gainedTrust = 0.05f + Mathf.Max(maxStat, 0f) / 0.3f * 0.01f;
                    }
                    curTrust = Mathf.Clamp01(curTrust + gainedTrust);
                    gainTrustCount++;
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_FamineVillage_GainTrust".Translate(gainedTrust.ToString("0.##"), curTrust.ToString("0.##")), Faction));
                    return;
                }
            case WorkType.PryInfo:
                {
                    if (Rand.Chance(curTrust))
                    {
                        validInfoCount++;
                        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_FamineVillage_PryInfoSuccess".Translate(), Faction));
                    }
                    else
                    {
                        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_FamineVillage_PryInfoFail".Translate(), Faction));
                    }
                    return;
                }
            case WorkType.Precise:
                {
                    FulfillRequest(associatedFixedCaravan);
                    if (!HasFeastLater && requestCountLeft <= 0)
                    {
                        this.SafeDestroy();
                    }
                    return;
                }
            case WorkType.Feast:
                {
                    ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("OARO_Thought_FamineVillagetFeast");
                    foreach (Pawn p in associatedFixedCaravan.PawnsListForReading)
                    {
                        p.needs?.mood.thoughts.memories.TryGainMemory(thoughtDef);
                        if (p.needs?.food is not null)
                        {
                            p.needs.food.CurLevelPercentage += 1f;
                        }
                    }
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_FamineVillage_FeastFinished".Translate(), Faction));
                    this.SafeDestroy();
                    return;
                }
            default: return;
        }
    }

    public override void PostConvertToCaravan(Caravan caravan)
    {
        switch (curWorkType)
        {
            case WorkType.Direct:
                QuestUtility.SendQuestTargetSignals(questTags, "RequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT), caravan.Named("CARAVAN"));
                return;
            case WorkType.Precise:
                if (HasFeastLater)
                {
                    Dialog_NodeTreeWithFactionInfo nodeTree = OAFrame_DiaUtility.ConfirmDiaNodeTreeWithFactionInfo(
                        text: "OARO_FamineVillage_FeastStart".Translate(),
                        faction: Faction,
                        acceptText: "Accept".Translate(),
                        acceptAction: delegate { StartWork(WorkType.Feast, caravan); });

                    Find.WindowStack.Add(nodeTree);
                    return;
                }
                if (requestCountLeft > 0)
                {
                    Dialog_NodeTreeWithFactionInfo nodeTree = OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                       text: "OARO_FamineVillage_FeastNonFinished".Translate(requestDef.label, requestCount - requestCountLeft, requestCountLeft),
                       faction: Faction);

                    Find.WindowStack.Add(nodeTree);
                    return;
                }
                else
                {
                    QuestUtility.SendQuestTargetSignals(questTags, "PerfectRequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT), caravan.Named("CARAVAN"));
                }
                return;
            case WorkType.Feast:
                QuestUtility.SendQuestTargetSignals(questTags, "PerfectRequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT), caravan.Named("CARAVAN"));
                return;
            default: return;
        }
    }

    public override void PreConvertToCaravanByPlayer()
    {
        if (curWorkType == WorkType.Feast)
        {
            OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_FamineVillage_FeastPlayerInterrupt".Translate(), Faction);
        }
    }

    protected override void InterruptWork()
    {
        if (curWorkType == WorkType.Feast)
        {
            if (associatedFixedCaravan is not null)
            {
                ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("OARO_Thought_FamineVillagetFeast");
                foreach (Pawn p in associatedFixedCaravan.PawnsListForReading)
                {
                    p.needs?.mood.thoughts.memories.TryGainMemory(thoughtDef);
                    if (p.needs?.food is not null)
                    {
                        p.needs.food.CurLevelPercentage += 1f;
                    }
                }
            }
            this.SafeDestroy();
        }
        else
        {
            OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_FamineVillage_WorkInterrupt".Translate(), Faction);
        }
    }

    public void FulfillRequest(FixedCaravan fixedCaravan)
    {
        int maxTakeCount = Mathf.Min(requestCountLeft, Mathf.CeilToInt(requestCount / 3 * validInfoCount));
        requestCountLeft -= fixedCaravan.RemoveThingsOfDef(requestDef, maxTakeCount);
        validInfoCount = 0;
        HasFeastLater = requestCountLeft <= 0 && curTrust >= 0.8f;
    }

    public void FulfillRequest(Caravan caravan)
    {
        caravan.RemoveThingsOfDef(requestDef, requestCountLeft);
        requestCountLeft = 0;
        QuestUtility.SendQuestTargetSignals(questTags, "RequestFulfilled", this.Named(KeyLibrary_FormatArgName.SUBJECT), caravan.Named("CARAVAN"));
        this.SafeDestroy();
    }
}
