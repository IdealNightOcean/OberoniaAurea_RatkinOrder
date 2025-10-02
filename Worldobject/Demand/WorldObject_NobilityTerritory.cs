using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;
using Verse.Utility;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 叛乱镇压 - 贵族领地
/// </summary>
internal sealed class WorldObject_NobilityTerritory : WorldObject_CriticalBranchDemand
{
    internal enum NobilityType : byte
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
        Infiltrate,
        Communication,
        Negotiate
    }

    public override int TicksNeeded => curWork switch
    {
        WorkType.Infiltrate => 20000,
        WorkType.Communication => nobilityType == NobilityType.Tyrannical ? 2500 : 20000,
        WorkType.Negotiate => 300000,
        _ => 20000
    };
    protected override int PeriodicCheckInterval => 60000;

    internal string NobilityCliqueKey => "Nobility_" + ID;
    internal string NobilityBureaucratCliqueKey => "NobilityBureaucrat_" + ID;
    internal string NobilityCivilianCliqueKey => "NobilityCivilian_" + ID;

    private string nobilityName;
    private NobilityType nobilityType;
    internal string NobilityName => nobilityName;
    internal NobilityType TypeOfNobility => nobilityType;

    private WorkType curWork;
    private int troops;
    internal int Troops => troops;
    private int TroopsExposed => GenMath.RoundTo(troops, 10);

    private bool hasExposeType;
    private bool hasExposeTroops;
    private bool hasNegotiated;
    private bool hasMapObject;
    private bool hasYield;

    private int ransomPlayer = -1;

    private float osmolity;
    internal float Osmolity
    {
        get => osmolity;
        private set => osmolity = Mathf.Clamp01(osmolity);
    }

    [Unsaved] private (bool assault, bool playerInitiated) assaultAfterWork = (false, false);

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

    public override void PostAdd()
    {
        base.PostAdd();

        GrammarRequest namerRequest = new()
        {
            Includes = { OARO_ModDefOf.OARO_Namer_Nobility }
        };
        nobilityName = GrammarResolver.Resolve("r_text", namerRequest);
        Name = nobilityName;

        QuestPart_NobilityTerritory_TypeSelecter questPart_NobilityTerritory_TypeSelecter = quest?.GetFirstPartOfType<QuestPart_NobilityTerritory_TypeSelecter>();
        if (questPart_NobilityTerritory_TypeSelecter is null)
        {
            (nobilityType, hasExposeType) = questPart_NobilityTerritory_TypeSelecter.PopAlternative();
        }
        if (nobilityType == NobilityType.None)
        {
            nobilityType = EnumUtility.GetValues<NobilityType>().Where(nt => nt != NobilityType.None).RandomElement();
            hasExposeType = false;
        }

        Osmolity = HasQuestEffectTag("WordGetsAround") ? 0.2f : 0f;
        troops = Rand.RangeInclusive(20, 80);
        if (HasQuestEffectTag("RebelBanner"))
        {
            troops += 20;
        }

        QuestClique nobilityClique = new()
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
        CliquesManager.TryAddClique(NobilityCliqueKey, nobilityClique);

        QuestClique bureaucratClique = new()
        {
            Name = "OARO_CliqueName_NobilityBureaucrat".Translate(Name),
            ActiveDesc = "OARO_CliqueActiveDesc_NobilityBureaucrat".Translate(),
            Potency = -0.075f,
            Willingness = 0f,

            IsActivatable = true,
            IsBribable = false,
            IsCommunicable = false
        };
        CliquesManager.TryAddClique(NobilityBureaucratCliqueKey, bureaucratClique, defaultActive: true);

        if (nobilityType == NobilityType.Kindness)
        {
            QuestClique civilianClique = new()
            {
                Name = "OARO_CliqueName_NobilityCivilian".Translate(Name),
                ActiveDesc = "OARO_CliqueActiveDesc_NobilityCivilian".Translate(),
                Potency = -0.075f,
                Willingness = 0f,

                IsActivatable = true,
                IsBribable = false,
                IsCommunicable = false
            };
            CliquesManager.TryAddClique(NobilityCivilianCliqueKey, civilianClique, defaultActive: true);
        }
    }

    internal void Notify_AssaultEnd(bool success)
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
            QuestUtility.SendQuestTargetSignals(questTags, "AssaultSucceed");
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
            QuestUtility.SendQuestTargetSignals(questTags, "AssaultFailed");
            this.SafeDestroy();
        }
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (hasMapObject)
        {
            Messages.Message("OARO_NobilityTerritory_Assaulting".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        if (hasYield)
        {
            Messages.Message("OARO_NobilityTerritory_HasYield".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Find.WindowStack.Add(new Dialog_NodeTree(ArrivedDiaNode(caravan)));
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

        Osmolity += osmolityChange;

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

        if (!hasMapObject)
        {
            DiaOption assaultOpt = new("OARO_NobilityTerritory_Assault".Translate())
            {
                action = delegate
                {
                    Assault(caravan, playerInitiated: true);
                },
                resolveTree = true
            };
            rootNode.options.Add(assaultOpt);
        }

        DiaOption communicationOpt = new("OARO_NobilityTerritory_Communication".Translate())
        {
            action = delegate
            {
                curWork = WorkType.Communication;
                base.Notify_CaravanArrived(caravan);
            },
            resolveTree = true
        };
        rootNode.options.Add(communicationOpt);

        if (hasNegotiated)
        {
            DiaOption secondRoundNegotiateOpt = new("OARO_NobilityTerritory_SecondRoundNegotiate".Translate())
            {
                linkLateBind = () => SecondRoundNegotiateDiaNode(caravan),
                resolveTree = false
            };
            rootNode.options.Add(secondRoundNegotiateOpt);
        }
        else
        {
            DiaOption negotiateOpt = new("OARO_NobilityTerritory_Negotiate".Translate())
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

        return rootNode;
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

        Osmolity += osmolityGain;

        if (!hasExposeType && Osmolity >= 0.2f)
        {
            hasExposeType = true;
            Find.LetterStack.ReceiveLetter(
                label: "OARO_NobilityTerritory_ExposeTypeLabel".Translate(),
                text: "OARO_NobilityTerritory_ExposeTypeText".Translate(nobilityName, $"OARO_NobilityTerritory_{nobilityName}".Translate()),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: this,
                relatedFaction: Faction,
                quest: quest);
        }
        if (!hasExposeTroops && Osmolity >= 0.5f)
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

    private void CommunicationResult()
    {
        assaultAfterWork = (assault: false, playerInitiated: false);
        if (CliquesManager.GetCliqueWillingness(NobilityCliqueKey) < 0.3f)
        {
            CliquesManager.AdjustCliqueWillingness(NobilityCliqueKey, 0.1f);
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                "OARO_NobilityTerritory_CommunicationResultLow".Translate(nobilityName, 0.1f.ToStringPercent("F2")),
                Faction));
        }
        else
        {
            if (nobilityType == NobilityType.Tyrannical)
            {
                assaultAfterWork = (assault: true, playerInitiated: false);
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
                "OARO_NobilityTerritory_CommunicationResult".Translate(nobilityName, willingnessChange.ToStringPercent("F2")),
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

    public override void PostConvertToCaravan(Caravan caravan)
    {
        base.PostConvertToCaravan(caravan);
        switch (curWork)
        {
            case WorkType.Communication:
                {
                    if (assaultAfterWork.assault)
                    {
                        Assault(caravan, assaultAfterWork.playerInitiated);
                    }
                    return;
                }
            case WorkType.Negotiate:
                {
                    if (hasNegotiated && ransomPlayer > 0)
                    {
                        Find.WindowStack.Add(new Dialog_NodeTree(FirstRoundNegotiateDiaNode(caravan)));
                    }
                    return;
                }
            default: return;
        }
    }

    private void Assault(Caravan caravan, bool playerInitiated)
    {
        MapParent_NobilityTerritory mapParent_NobilityTerritory = (MapParent_NobilityTerritory)WorldObjectMaker.MakeWorldObject(OARO_ModDefOf.OARO_Map_NobilityTerritory);
        mapParent_NobilityTerritory.SetFaction(Faction);
        mapParent_NobilityTerritory.Tile = Tile;
        mapParent_NobilityTerritory.InitRaidInfo(this, troops, playerInitiated);
        Find.WorldObjects.Add(mapParent_NobilityTerritory);
        new CaravanArrivalAction_GenerateAndEnter().Arrived(caravan);
        hasMapObject = true;
    }

    /// <summary>
    /// 首次谈判结果界面（需要遣散费）
    /// </summary>
    private DiaNode FirstRoundNegotiateDiaNode(Caravan caravan)
    {
        DiaNode diaNode = new("OARO_NobilityTerritory_FirstNegotiateResult_Info".Translate(nobilityName, ransomPlayer * 10, ransomPlayer));

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

    private DiaNode SecondRoundNegotiateDiaNode(Caravan caravan)
    {
        DiaNode diaNode = new("OARO_NobilityTerritory_SecondNegotiateResult_Info".Translate(nobilityName, ransomPlayer * 10, ransomPlayer));
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

        DiaOption waitOpt = new("OARO_NobilityTerritory_SecondNegotiateResult_Wait".Translate())
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
        QuestUtility.SendQuestTargetSignals(questTags, "NobilityYield");
    }

    protected override void Reset()
    {
        base.Reset();
        assaultAfterWork = (false, false);
    }

}

/// <summary>
/// 叛乱镇压 - 贵族领地 攻击时的地图
/// </summary>
internal sealed class MapParent_NobilityTerritory : MapParent
{
    internal enum AssaultType
    {
        BePounced,
        Normal,
        Pounce,
        DeadlyPounce
    }

    internal WorldObject_NobilityTerritory Parent;
    internal AssaultType TypeOfAssault;

    private bool succeeded;

    internal void InitRaidInfo(WorldObject_NobilityTerritory parent, int troops, bool playerInitiated)
    {
        Parent = parent;
        if (playerInitiated)
        {
            TypeOfAssault = parent.Osmolity > 0.999f ? AssaultType.DeadlyPounce : Rand.Chance(parent.Osmolity) ? AssaultType.Pounce : AssaultType.Normal;
        }
        else
        {
            TypeOfAssault = AssaultType.BePounced;
        }
    }

    public override void PostMapGenerate()
    {
        base.PostMapGenerate();
        try
        {
            LetterDef letterDef = TypeOfAssault switch
            {
                AssaultType.BePounced => LetterDefOf.ThreatBig,
                AssaultType.Normal => LetterDefOf.NeutralEvent,
                _ => LetterDefOf.PositiveEvent
            };

            Find.LetterStack.ReceiveLetter(
                label: $"OARO_NobilityTerritory_AssaultLabel_{TypeOfAssault}".Translate(),
                text: $"OARO_NobilityTerritory_AssaultText_{TypeOfAssault}".Translate(Parent.NobilityName),
                textLetterDef: letterDef,
                lookTargets: this,
                quest: Parent.AssociatedQuest);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in {nameof(PostMapGenerate)}: {ex.Message}");
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Parent, "Parent");
        Scribe_Values.Look(ref TypeOfAssault, "TypeOfAssault");

        Scribe_Values.Look(ref succeeded, "succeeded", defaultValue: false);
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (HasMap && !GenHostility.AnyHostileActiveThreatToPlayer(Map, countDormantPawnsAsHostile: true))
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