using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Grammar;
using Verse.Utility;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 叛乱镇压 - 贵族领地
/// </summary>
public sealed class WorldObject_NobilityTerritory : WorldObject_CriticalBranchDemand
{
    public enum NobilityType : byte
    {
        None,
        Deceitful, //狡诈
        Stubborn, //刚愎
        Justice, //正义
        Tyrannical, //暴虐
        Kindness, //仁慈
        Greediness //贪婪
    }

    private enum WorkType : byte
    {
        Infiltrate, //尝试渗透
        Communication, //与贵族交流
        Negotiate //与贵族谈判
    }

    public override int TicksNeeded => curWork switch
    {
        WorkType.Infiltrate => 20000,
        WorkType.Communication => nobilityType == NobilityType.Tyrannical ? 2500 : 20000,
        WorkType.Negotiate => 30000,
        _ => 20000
    };
    protected override int PeriodicCheckInterval => 60000;

    public string NobilityCliqueKey => "Nobility_" + ID;
    public string NobilityBureaucratCliqueKey => "NobilityBureaucrat_" + ID;
    public string NobilityCivilianCliqueKey => "NobilityCivilian_" + ID;

    private string nobilityName;
    private NobilityType nobilityType;
    public string NobilityName => nobilityName;
    public NobilityType NobilityTypeValue => nobilityType;

    private WorkType curWork;
    private int troops;
    public int Troops => troops;
    private int TroopsExposed => GenMath.RoundTo(troops, 10);

    private bool hasExposeType;
    private bool hasExposeTroops;
    private bool hasNegotiated;
    private bool hasMapObject;
    private bool hasYield;
    public bool HasYield => hasYield;

    private int ransomPlayer = -1;

    private float osmolity;
    public float Osmolity => osmolity;

    [Unsaved] private bool beAssaultedAfterWork = false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nobilityName, "nobilityName");
        Scribe_Values.Look(ref curWork, "curWork");
        Scribe_Values.Look(ref nobilityType, "nobilityType");
        Scribe_Values.Look(ref troops, "troops", 0);

        Scribe_Values.Look(ref hasExposeType, "hasExposeType", defaultValue: false);
        Scribe_Values.Look(ref hasExposeTroops, "hasExposeTroops", defaultValue: false);
        Scribe_Values.Look(ref hasNegotiated, "hasNegotiated", defaultValue: false);
        Scribe_Values.Look(ref hasMapObject, "hasMapObject", defaultValue: false);
        Scribe_Values.Look(ref hasYield, "hasYield", defaultValue: false);

        Scribe_Values.Look(ref ransomPlayer, "ransomPlayer", -1);

        Scribe_Values.Look(ref osmolity, "osmolity", 0f);
    }

    public void InitNobilityTerritory(NobilityType nobilityType, bool hasExposed)
    {
        GrammarRequest namerRequest = new()
        {
            Includes = { OARO_RulePackDefOf.OARO_Namer_Nobility }
        };
        nobilityName = GrammarResolver.Resolve("r_name", namerRequest);
        Name = nobilityName;

        osmolity = HasQuestEffectTag("WordGetsAround") ? 0.2f : 0f;
        troops = Rand.RangeInclusive(20, 80);
        this.nobilityType = nobilityType;
        hasExposeType = hasExposed;

        if (nobilityType == NobilityType.Kindness)
        {
            QuestClique civilianClique = new(NobilityCivilianCliqueKey)
            {
                Name = "OARO_CliqueName_NobilityCivilian".Translate(Name),
                ActiveDesc = "OARO_CliqueActiveDesc_NobilityCivilian".Translate(),
                Potency = -0.075f,
                Willingness = 0f,

                IsActivatable = true,
                IsBribable = false,
                IsCommunicable = false
            };
            CliquesManager.TryAddClique(civilianClique, defaultActive: true);
        }

        if (HasQuestEffectTag("RebelBanner"))
        {
            troops += 20;
        }

        QuestClique nobilityClique = new(NobilityCliqueKey)
        {
            Name = nobilityName,
            ActiveDesc = "OARO_CliqueActiveDesc_Nobility".Translate(),
            InactiveDesc = "OARO_CliqueInactiveDesc_Nobility".Translate(),
            Potency = 0.2f,
            Willingness = 0f,

            IsActivatable = true,
            IsBribable = false,
            IsCommunicable = false
        };
        CliquesManager.TryAddClique(nobilityClique);

        QuestClique bureaucratClique = new(NobilityBureaucratCliqueKey)
        {
            Name = "OARO_CliqueName_NobilityBureaucrat".Translate(Name),
            ActiveDesc = "OARO_CliqueActiveDesc_NobilityBureaucrat".Translate(),
            Potency = -0.075f,
            Willingness = 0f,

            IsActivatable = true,
            IsBribable = false,
            IsCommunicable = false
        };
        CliquesManager.TryAddClique(bureaucratClique, defaultActive: true);
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());

        if (hasYield)
        {
            sb.AppendInNewLine("OARO_NobilityTerritory_HasYield".Translate(nobilityName));
            return sb.ToString();
        }

        if (hasExposeType)
        {
            sb.AppendInNewLine("OARO_NobilityTerritory_NobilityType".Translate());
            sb.Append(": ");
            sb.Append($"OARO_NobilityTerritory_{nobilityType}".Translate());
        }
        if (hasExposeTroops)
        {
            sb.AppendInNewLine("OARO_NobilityTerritory_Troops".Translate(TroopsExposed));
        }
        sb.AppendInNewLine("OARO_NobilityTerritory_Osmolity".Translate(osmolity.ToStringPercent("0.##")));
        if (isWorking)
        {
            sb.AppendInNewLine("OARO_WorldObejct_CurWork".Translate());
            sb.Append(": ");
            sb.Append($"OARO_NobilityTerritory_{curWork}".Translate());
        }

        return sb.ToString();
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
            defaultLabel = "DEV: Add 10% osmolity",
            action = delegate
            {
                AdjustOsmolity(0.1f);
            }
        };
        yield return new Command_Action()
        {
            defaultLabel = "DEV: Yield nobility",
            action = NobilityYield
        };
        yield return new Command_Action()
        {
            defaultLabel = "DEV: Add 10% nobility willingness",
            action = delegate
            {
                CliquesManager.AdjustCliqueWillingness(NobilityCliqueKey, 0.1f);
            }
        };
    }

    public void Notify_AssaultEnd(bool success)
    {
        if (success)
        {
            if (CliquesManager is not null)
            {
                cliquesManager.RemoveClique(NobilityCliqueKey);
                cliquesManager.RemoveClique(NobilityBureaucratCliqueKey);
                cliquesManager.RemoveClique(NobilityCivilianCliqueKey);
            }
            Find.LetterStack.ReceiveLetter(
                label: "OARO_NobilityTerritory_AssaultSucceedLabel".Translate(),
                text: "OARO_NobilityTerritory_AssaultSucceedText".Translate(nobilityName),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: this,
                quest: quest);
            SendWorkResolvedSignal([false.Named("YIELD")]);
            this.SafeDestroy();
        }
        else
        {
            Find.LetterStack.ReceiveLetter(
                label: "OARO_NobilityTerritory_AssaultFailedLabel".Translate(),
                text: "OARO_NobilityTerritory_AssaultFailedText".Translate(nobilityName),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: this,
                quest: quest);
            QuestUtility.SendQuestTargetSignals(questTags, "AssaultFailed", this.Named("SUBJECT"));
            this.SafeDestroy();
        }
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (hasMapObject)
        {
            Messages.Message("OARO_NobilityTerritory_AssaultingMess".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        if (hasYield)
        {
            Messages.Message("OARO_NobilityTerritory_HasYieldMess".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Find.WindowStack.Add(new Dialog_NodeTree(ArrivedDiaNode(caravan)));
    }

    private DiaNode ArrivedDiaNode(Caravan caravan)
    {
        DiaNode rootNode = new("OARO_NobilityTerritory_ArrivalInfo".Translate(Name));

        DiaOption infiltrateOpt = new("OARO_NobilityTerritory_Infiltrate".Translate())
        {
            action = delegate
            {
                curWork = WorkType.Infiltrate;
                base.Notify_CaravanArrived(caravan);
            },
            resolveTree = true
        };
        rootNode.options.Add(infiltrateOpt);


        DiaOption assaultOpt = new("OARO_NobilityTerritory_Assault".Translate())
        {
            linkLateBind = () => AssaultNode(caravan),
            resolveTree = false
        };
        rootNode.options.Add(assaultOpt);

        if (!isWorking)
        {
            foreach (WorkType workType in EnumUtility.GetValues<WorkType>())
            {
                if (workType == WorkType.Negotiate)
                {
                    continue;
                }

                DiaOption workOpt = new($"OARO_NobilityTerritory_{workType}".Translate())
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

        if (hasNegotiated)
        {
            DiaOption ransomOpt = new("OARO_NobilityTerritory_RansomOpt".Translate())
            {
                linkLateBind = () => RansomDiaNode(caravan),
                resolveTree = false
            };
            rootNode.options.Add(ransomOpt);
        }
        else if (!isWorking)
        {
            DiaOption negotiateOpt = new($"OARO_NobilityTerritory_{WorkType.Negotiate}".Translate())
            {
                action = delegate
                {
                    curWork = WorkType.Negotiate;
                    base.Notify_CaravanArrived(caravan);
                },
                resolveTree = true
            };
            if (CliquesManager.GetCliqueWillingness(NobilityCliqueKey) < 0.5f)
            {
                negotiateOpt.Disable("OARO_Insufficient_CliqueWillingness".Translate(nobilityName, 0.5f.ToStringPercent("f2")));
            }
            rootNode.options.Add(negotiateOpt);
        }

        rootNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);

        return rootNode;
    }

    protected override void PeriodicCheck()
    {
        float osmolityChange = 0f;
        if (!HasQuestEffectTag("WordGetsAround"))
        {
            if (CliquesManager.IsCliqueActive(NobilityBureaucratCliqueKey))
            {
                osmolityChange -= Rand.Range(0.02f, 0.05f);
            }
            if (CliquesManager.IsCliqueActive(NobilityCivilianCliqueKey))
            {
                osmolityChange -= Rand.Range(0.03f, 0.05f);
            }
            if (CliquesManager.IsCliqueActive("MachiavellianBureaucrat"))
            {
                osmolityChange -= Rand.Range(0.01f, 0.03f);
            }
        }
        if (CliquesManager.IsCliqueActive("LoyalBureaucrat"))
        {
            osmolityChange += Rand.Range(0.01f, 0.03f);
        }

        AdjustOsmolity(osmolityChange);
    }

    protected override void FinishWork()
    {
        switch (curWork)
        {
            case WorkType.Infiltrate:
                {
                    InfiltrateResult();
                    break;
                }
            case WorkType.Communication:
                {
                    CommunicationResult();
                    break;
                }
            case WorkType.Negotiate:
                {
                    NegotiateResult();
                    break;
                }
            default: break;
        }
    }

    protected override void InterruptWork() { }

    private void InfiltrateResult()
    {
        float osmolityGain = 0.05f + Mathf.Max(0f, TotalPotency * 0.2f);
        osmolityGain += 0.25f * (OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Melee)
                              + OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Intellectual));
        osmolityGain += nobilityType switch
        {
            NobilityType.Deceitful => -0.08f,
            NobilityType.Stubborn => -0.04f,
            NobilityType.Tyrannical => 0.03f,
            NobilityType.Justice => 0.03f,
            NobilityType.Kindness => 0.05f,
            _ => 0f
        };
        if (CliquesManager.IsCliqueActive("Thief"))
        {
            osmolityGain += 0.1f;
        }
        else if (CliquesManager.IsCliqueActive("TimidThief"))
        {
            osmolityGain += 0.05f;
        }
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
               "OARO_NobilityTerritory_InfiltrateResult".Translate(osmolityGain.ToStringPercent("0.##")),
               Faction));
        AdjustOsmolity(osmolityGain);
    }

    private void CommunicationResult()
    {
        beAssaultedAfterWork = false;
        if (CliquesManager.GetCliqueWillingness(NobilityCliqueKey) < 0.3f)
        {
            CliquesManager.AdjustCliqueWillingness(NobilityCliqueKey, 0.1f);
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                "OARO_NobilityTerritory_CommunicationResultLow".Translate(nobilityName, 0.1f.ToStringPercent("0.##")),
                Faction));
        }
        else
        {
            if (nobilityType == NobilityType.Tyrannical)
            {
                beAssaultedAfterWork = true;
                return;
            }

            float willingnessChange = 0.05f
                + TotalPotency * 0.1f
                + 0.005f * OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Social);

            willingnessChange += nobilityType switch
            {
                NobilityType.Deceitful => -0.1f,
                NobilityType.Stubborn => -0.15f,
                NobilityType.Justice => 0.05f,
                NobilityType.Kindness => 0.1f,
                _ => 0f
            };
            if (HasQuestEffectTag("RebelBanner") && (nobilityType != NobilityType.Justice && nobilityType != NobilityType.Kindness))
            {
                willingnessChange -= 0.08f;
            }
            if (hasExposeType)
            {
                willingnessChange += 0.05f;
            }
            willingnessChange = Mathf.Max(0f, willingnessChange);

            CliquesManager.AdjustCliqueWillingness(NobilityCliqueKey, willingnessChange);
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                "OARO_NobilityTerritory_CommunicationResult".Translate(nobilityName, willingnessChange.ToStringPercent("0.##")),
                Faction));
        }
    }

    private void NegotiateResult()
    {
        float willingness = CliquesManager.GetCliqueWillingness(NobilityCliqueKey);
        hasNegotiated = true;

        if (willingness > 0.999f && (nobilityType == NobilityType.Justice || nobilityType == NobilityType.Kindness))
        {
            ransomPlayer = -1;
            Dialog_NodeTree nodeTree = OAFrame_DiaUtility.ConfirmDiaNodeTree("OARO_NobilityTerritory_NegotiateResult_NoRansom".Translate(nobilityName), "Accept".Translate(), NobilityYield);
            Find.WindowStack.Add(nodeTree);
        }
        else
        {
            float ransom = 30000f;
            ransom *= (3f - 2f * CliquesManager.GetCliqueWillingness(NobilityCliqueKey));
            ransom *= nobilityType switch
            {
                NobilityType.Stubborn => 1.5f,
                NobilityType.Kindness => 0.5f,
                NobilityType.Greediness => 2f,
                _ => 1f
            };

            if (HasQuestEffectTag("RansomPreparation"))
            {
                ransomPlayer = -1;
                int ransomInt = Mathf.Max(1, Mathf.FloorToInt(ransom));
                Dialog_NodeTree nodeTree = OAFrame_DiaUtility.ConfirmDiaNodeTree(
                    text: "OARO_NobilityTerritory_NegotiateResult_ReadyRansom".Translate(nobilityName, ransomInt),
                    acceptText: "Accept".Translate(),
                    acceptAction: NobilityYield);
                Find.WindowStack.Add(nodeTree);
            }
            else
            {
                ransomPlayer = Mathf.Max(1, Mathf.FloorToInt(ransom * 0.1f));
            }
        }
    }

    private void AdjustOsmolity(float change)
    {
        osmolity = Mathf.Clamp01(osmolity + change);

        if (!hasExposeType && osmolity >= 0.2f)
        {
            hasExposeType = true;
            Find.LetterStack.ReceiveLetter(
                label: "OARO_NobilityTerritory_ExposeTypeLabel".Translate(),
                text: "OARO_NobilityTerritory_ExposeTypeText".Translate(nobilityName, $"OARO_NobilityTerritory_{nobilityType}".Translate()),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: this,
                relatedFaction: Faction,
                quest: quest);
        }
        if (!hasExposeTroops && osmolity >= 0.5f)
        {
            hasExposeTroops = true;
            Find.LetterStack.ReceiveLetter(
                label: "OARO_NobilityTerritory_ExposeTroopsLabel".Translate(),
                text: "OARO_NobilityTerritory_ExposeTroopsText".Translate(nobilityName, TroopsExposed),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: this,
                relatedFaction: Faction,
                quest: quest);
        }
    }

    public override void PostConvertToCaravan(Caravan caravan)
    {
        base.PostConvertToCaravan(caravan);
        switch (curWork)
        {
            case WorkType.Communication:
                {
                    if (beAssaultedAfterWork)
                    {
                        Assault(caravan, playerInitiated: false, branchJoin: true);
                    }
                    return;
                }
            case WorkType.Negotiate:
                {
                    if (hasNegotiated && ransomPlayer > 0)
                    {
                        Find.WindowStack.Add(new Dialog_NodeTree(NegotiateDiaNode(caravan)));
                    }
                    return;
                }
            default: return;
        }
    }

    private DiaNode AssaultNode(Caravan caravan)
    {
        TaggedString nodeText = "OARO_NobilityTerritory_AssaultInfo".Translate(nobilityName, osmolity.ToStringPercent("0.##"), Branch.Name.Named("BRANCH"));
        if (hasExposeTroops)
        {
            nodeText += "\n\n" + "OARO_NobilityTerritory_Troops".Translate(troops);

        }
        DiaNode diaNode = new(nodeText);
        DiaOption jointOpt = new("OARO_NobilityTerritory_Assault_Joint".Translate())
        {
            action = () => Assault(caravan, playerInitiated: true, branchJoin: true),
            resolveTree = true
        };
        diaNode.options.Add(jointOpt);

        DiaOption independentOpt = new("OARO_NobilityTerritory_Assault_Independent".Translate())
        {
            action = () => Assault(caravan, playerInitiated: true, branchJoin: false),
            resolveTree = true
        };
        diaNode.options.Add(independentOpt);

        diaNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);

        return diaNode;
    }

    private void Assault(Caravan caravan, bool playerInitiated, bool branchJoin)
    {
        MapParent_NobilityTerritory nobilityTerritory = (MapParent_NobilityTerritory)WorldObjectMaker.MakeWorldObject(OARO_WorldObjectDefOf.OARO_Map_NobilityTerritory);
        nobilityTerritory.SetFaction(Faction);
        nobilityTerritory.Tile = Tile;
        nobilityTerritory.InitRaidInfo(this, playerInitiated, branchJoin);
        Find.WorldObjects.Add(nobilityTerritory);
        new CaravanArrivalAction_GenerateAndEnter(nobilityTerritory).Arrived(caravan);
        hasMapObject = true;
    }

    private DiaNode NegotiateDiaNode(Caravan caravan)
    {
        DiaNode diaNode = new("OARO_NobilityTerritory_NegotiateResult_Info".Translate(nobilityName, ransomPlayer * 10, ransomPlayer));

        DiaOption payOpt = new("OARO_NobilityTerritory_NegotiateResult_Pay".Translate())
        {
            action = delegate
            {
                caravan.RemoveThingsOfDef(ThingDefOf.Silver, ransomPlayer);
                NobilityYield();
            },
            resolveTree = true
        };
        if (!CaravanInventoryUtility.HasThings(caravan, ThingDefOf.Silver, ransomPlayer))
        {
            payOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, ransomPlayer));
        }
        diaNode.options.Add(payOpt);

        DiaOption waitOpt = new("OARO_NobilityTerritory_NegotiateResult_Wait".Translate())
        {
            resolveTree = true
        };
        diaNode.options.Add(waitOpt);

        return diaNode;
    }

    private DiaNode RansomDiaNode(Caravan caravan)
    {
        DiaNode diaNode = new("OARO_NobilityTerritory_RansomInfo".Translate(nobilityName, ransomPlayer * 10, ransomPlayer));
        DiaOption payOpt = new("OARO_NobilityTerritory_Ransom_Pay".Translate())
        {
            action = delegate
            {
                caravan.RemoveThingsOfDef(ThingDefOf.Silver, ransomPlayer);
                NobilityYield();
            },
            resolveTree = true
        };
        if (!CaravanInventoryUtility.HasThings(caravan, ThingDefOf.Silver, ransomPlayer))
        {
            payOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, ransomPlayer));
        }

        diaNode.options.Add(payOpt);

        DiaOption waitOpt = new("OARO_NobilityTerritory_Ransom_Wait".Translate())
        {
            resolveTree = true
        };
        diaNode.options.Add(waitOpt);
        return diaNode;
    }

    private void NobilityYield()
    {
        hasYield = true;
        if (CliquesManager is not null)
        {
            cliquesManager.TryActiveClique(NobilityCliqueKey, directly: true);
            cliquesManager.RemoveClique(NobilityBureaucratCliqueKey);
            cliquesManager.RemoveClique(NobilityCivilianCliqueKey);
        }
        if (isWorking)
        {
            EndWork(interrupt: true);
        }
        SendWorkResolvedSignal([true.Named("YIELD")]);
    }

    protected override void Reset()
    {
        base.Reset();
        beAssaultedAfterWork = false;
    }

}

/// <summary>
/// 叛乱镇压 - 贵族领地 攻击时的地图
/// </summary>
public sealed class MapParent_NobilityTerritory : MapParent_Enterable
{
    public enum AssaultType
    {
        BePounced,
        Normal,
        Pounce,
        DeadlyPounce
    }

    public WorldObject_NobilityTerritory Parent;
    public AssaultType AssaultTypeValue;
    public bool BranchJoin;

    private bool succeeded;

    public void InitRaidInfo(WorldObject_NobilityTerritory parent, bool playerInitiated, bool branchJoin)
    {
        Parent = parent;
        BranchJoin = branchJoin;
        if (playerInitiated)
        {
            AssaultTypeValue = parent.Osmolity > 0.999f ? AssaultType.DeadlyPounce : Rand.Chance(parent.Osmolity) ? AssaultType.Pounce : AssaultType.Normal;
        }
        else
        {
            AssaultTypeValue = AssaultType.BePounced;
        }
    }

    public override void PostMapGenerate()
    {
        base.PostMapGenerate();
        try
        {
            LetterDef letterDef = AssaultTypeValue switch
            {
                AssaultType.BePounced => LetterDefOf.ThreatBig,
                AssaultType.Normal => LetterDefOf.NeutralEvent,
                _ => LetterDefOf.PositiveEvent
            };

            Find.LetterStack.ReceiveLetter(
                label: $"OARO_NobilityTerritory_AssaultLabel_{AssaultTypeValue}".Translate(),
                text: $"OARO_NobilityTerritory_AssaultText_{AssaultTypeValue}".Translate(Parent.NobilityName),
                textLetterDef: letterDef,
                lookTargets: this,
                quest: Parent.AssociatedQuest);

            IncidentParms parms = new()
            {
                target = Map,
                faction = Faction,
                forced = true
            };
            OAFrame_MiscUtility.AddNewQueuedIncident(OARO_ModDefOf.OARO_RaidNobilityTerritory, delayTicks: 60, parms);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "generating Nobility Territory assault map.",
                typeName: nameof(MapParent_NobilityTerritory),
                methodName: nameof(PostMapGenerate),
                needStackTrace: true);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Parent, "Parent");
        Scribe_Values.Look(ref AssaultTypeValue, "AssaultTypeValue");
        Scribe_Values.Look(ref BranchJoin, "BranchJoin");

        Scribe_Values.Look(ref succeeded, "succeeded", defaultValue: false);
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (!succeeded && HasMap && !GenHostility.AnyHostileActiveThreatToPlayer(Map, countDormantPawnsAsHostile: true))
        {
            succeeded = true;
            forceRemoveWorldObjectWhenMapRemoved = true;
            Parent?.Notify_AssaultEnd(true);
        }
    }

    public override void Destroy()
    {
        if (!succeeded)
        {
            Parent?.Notify_AssaultEnd(false);
        }
        base.Destroy();
    }
}