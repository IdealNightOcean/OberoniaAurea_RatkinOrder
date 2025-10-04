using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 难民潮 - 难民营地
/// </summary>
public class WorldObject_RefugeeInfluxCamp : WorldObject_CriticalBranchDemand
{
    private enum PolicyType : byte
    {
        FocusPacify,
        ImproveDist,
    }
    private enum WorkType : byte
    {
        AssistPacify,
        Hunting
    }

    private bool HasDisorderlyMilitaryTag => HasQuestEffectTag("DisorderlyMilitary");
    public string RefugeeCliqueKey => "Refugee_" + ID;
    public string RoyalArmyCliqueKey => "RoyalArmy_" + ID;

    public override int TicksNeeded => 30000;
    protected override int PeriodicCheckInterval => 60000;

    private bool onMilitarySupervision;

    private PolicyType curPolicy;
    private WorkType curWork;

    private int originalPopulation;
    private int population;
    private float yestPopulationChange;

    private float distEfficiency;
    private float extraFixeddistEfficiency;
    private float yestDistEfficiencyChange;
    private bool forceDist;

    private float famineRisk;
    private float yestFamineRiskChange;

    private CooldownRecordManager cooldownManager = new();

    private float DistEfficiency
    {
        get
        {
            if (forceDist)
            {
                return 1f;
            }
            return distEfficiency;
        }
        set
        {
            float oldDistEfficiency = distEfficiency;
            distEfficiency = Mathf.Clamp01(value);
            yestDistEfficiencyChange += (distEfficiency - oldDistEfficiency);
        }
    }
    public float FamineRisk
    {
        get => famineRisk;
        private set
        {
            float oldFamineRisk = famineRisk;
            famineRisk = Mathf.Clamp01(value);
            yestFamineRiskChange += (famineRisk - oldFamineRisk);
        }
    }
    private int FamineRiskLevel => famineRisk switch
    {
        < 0.4f => 0,
        < 0.7f => 1,
        < 0.99f => 2,
        _ => 3,
    };

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref onMilitarySupervision, "onMilitarySupervision", defaultValue: false);

        Scribe_Values.Look(ref curPolicy, "curPolicy");
        Scribe_Values.Look(ref curWork, "curWork");

        Scribe_Values.Look(ref population, "population", 0);
        Scribe_Values.Look(ref yestPopulationChange, "yestPopulationChange", 0f);

        Scribe_Values.Look(ref distEfficiency, "distEfficiency", 0f);
        Scribe_Values.Look(ref extraFixeddistEfficiency, "extraFixeddistEfficiency", 0f);
        Scribe_Values.Look(ref yestDistEfficiencyChange, "yestDistEfficiencyChange", 0f);
        Scribe_Values.Look(ref forceDist, "forceDist", defaultValue: false);

        Scribe_Values.Look(ref famineRisk, "famineRisk", 0f);
        Scribe_Values.Look(ref yestFamineRiskChange, "yestFamineRiskChange", 0f);

        Scribe_Deep.Look(ref cooldownManager, "cooldownManager", 0);
    }

    public override void PostAdd()
    {
        base.PostAdd();
        originalPopulation = Rand.RangeInclusive(1000, 1500);
        population = originalPopulation;
        distEfficiency = 0.5f;
        famineRisk = 0f;
        cooldownManager.RegisterRecord("PeriodicCheck", cdTicks: 30000, shouldRemoveWhenExpired: true);
        cooldownManager.RegisterRecord("GrainArrival", cdTicks: 5 * 30000, shouldRemoveWhenExpired: true);

        QuestClique refugeeClique = new(RefugeeCliqueKey)
        {
            Name = "OARO_CliqueName_Refugee".Translate(Name),
            ActiveDesc = "OARO_CliqueActiveDesc_Refugee".Translate(),
            InactiveDesc = "OARO_CliqueInactiveDesc_Refugee".Translate(),
            Potency = 0.2f,
            Willingness = Rand.Range(0.15f, 0.25f),
            IsActivatable = true,
            IsCommunicable = true,
            IsBribable = false,

            PreferredBuilding = BranchBuildingDefOf.OARO_Church
        };

        QuestClique royalArmyClique = new(RoyalArmyCliqueKey)
        {
            Name = "OARO_CliqueName_RoyalArmy".Translate(Name),
            ActiveDesc = "OARO_CliqueActiveDesc_RoyalArmy".Translate(),
            InactiveDesc = "OARO_CliqueInactiveDesc_RoyalArmy".Translate(),
            Potency = -0.35f,
            IsActivatable = true,
            IsCommunicable = false,
            IsBribable = false,
        };

        if (HasDisorderlyMilitaryTag)
        {
            royalArmyClique.Potency = -0.5f;
        }

        CliquesManager.TryAddClique(refugeeClique);
        CliquesManager.TryAddClique(royalArmyClique);
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine("OARO_WorldObejct_CurPolicy".Translate());
        sb.Append(": ");
        sb.Append($"OARO_RefugeeInflux_{curPolicy}".Translate());

        if (isWorking)
        {
            sb.AppendInNewLine("OARO_WorldObejct_CurWork".Translate());
            sb.Append(": ");
            sb.Append($"OARO_RefugeeInflux_{curWork}".Translate());
        }

        sb.AppendInNewLine("OARO_WorldObejct_PopulationInfo".Translate(population, originalPopulation));

        sb.AppendInNewLine("OARO_RefugeeInflux_DistEfficiency".Translate(distEfficiency.ToStringPercent("F2")));
        sb.AppendInNewLine("OARO_RefugeeInflux_DistEfficiencyDesc".Translate());

        Color famineRiskColor = FamineRiskLevel switch
        {
            0 => Color.green,
            1 => Color.yellow,
            2 => ColorLibrary.Orange,
            _ => ColorLibrary.RedReadable,
        };
        sb.AppendInNewLine("OARO_RefugeeInflux_FamineRisk".Translate(famineRisk.ToStringPercent("F2")).Colorize(famineRiskColor));
        sb.AppendInNewLine($"OARO_RefugeeInflux_FamineRiskDesc_{FamineRiskLevel}".Translate().Colorize(famineRiskColor));

        return sb.ToString();
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        Find.WindowStack.Add(new Dialog_NodeTree(ArrivedDiaNode(caravan)));
    }

    private DiaNode ArrivedDiaNode(Caravan caravan)
    {
        TaggedString text = onMilitarySupervision ? "OARO_RefugeeInflux_ArrivalInfo_MS".Translate() : "OARO_RefugeeInflux_ArrivalInfo".Translate();
        DiaNode rootNode = new(text);

        if (!onMilitarySupervision)
        {
            DiaOption exileOpt = new("OARO_RefugeeInflux_Exile".Translate())
            {
                action = ExileRefugees,
                resolveTree = true
            };
            rootNode.options.Add(exileOpt);

            DiaOption supervisionOpt = new("OARO_RefugeeInflux_MilitarySupervision".Translate())
            {
                action = MilitaryControl,
                resolveTree = true
            };
            rootNode.options.Add(exileOpt);

            DiaOption distributionOpt = new("OARO_RefugeeInflux_DistributionFood".Translate())
            {
                linkLateBind = () => DistributionNode(caravan),
                resolveTree = false
            };
            int cooldownTicksLeft = cooldownManager.GetCooldownTicksLeft("DistributionFood");
            if (cooldownTicksLeft > 0)
            {
                distributionOpt.Disable("WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod()));
            }
            rootNode.options.Add(distributionOpt);

            if (!isWorking)
            {
                DiaOption huntingOpt = new("OARO_RefugeeInflux_Hunting".Translate())
                {
                    action = delegate
                    {
                        curWork = WorkType.Hunting;
                        base.Notify_CaravanArrived(caravan);
                    },
                    resolveTree = true
                };
                rootNode.options.Add(huntingOpt);

                DiaOption pacifyOpt = new("OARO_RefugeeInflux_AssistPacify".Translate())
                {
                    action = delegate
                    {
                        curWork = WorkType.AssistPacify;
                        base.Notify_CaravanArrived(caravan);
                    },
                    resolveTree = true
                };
                rootNode.options.Add(pacifyOpt);
            }
        }

        rootNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);
        return rootNode;
    }

    /// <summary>
    /// 先检测饥荒削减人口，未导致失败再进行后继行为
    /// </summary>
    protected override void PeriodicCheck()
    {
        FamineCheck();
        if (Destroyed)
        {
            return;
        }

        yestDistEfficiencyChange = 0f;
        yestFamineRiskChange = 0f;
        yestPopulationChange = 0f;

        RecacheExtraFixeddistEfficiency();

        if (!onMilitarySupervision && curPolicy == PolicyType.FocusPacify)
        {
            AdjuestRefugeeWillingness(0.05f + TotalPotency * 0.1f);
        }


        float famineRiskChange = Rand.Range(0.08f, 0.16f);
        famineRiskChange += (1f - DistEfficiency);
        FamineRisk += famineRiskChange;

        if (!cooldownManager.IsInCooldown("GrainArrival"))
        {
            GrainArrival();
        }
    }

    private void FamineCheck()
    {
        switch (FamineRiskLevel)
        {
            case 1:
                {
                    AdjuestRefugeeWillingness(-0.01f);
                    break;
                }
            case 2:
                {
                    AdjuestRefugeeWillingness(-0.02f);
                    AdjustPopulation(Mathf.FloorToInt(10 + population * 0.01f));
                    break;
                }
            case 3:
                {
                    AdjuestRefugeeWillingness(-0.05f);
                    AdjustPopulation(Mathf.FloorToInt(10 + population * 0.05f));
                    break;
                }
            default: break;
        }
    }

    /// <summary>
    /// 运粮队到达
    /// </summary>
    private void GrainArrival()
    {
        int grainCdTicks = HasQuestEffectTag("RemoteArea") ? 6 * 60000 : 5 * 60000;
        cooldownManager.RegisterRecord("GrainArrival", cdTicks: grainCdTicks, shouldRemoveWhenExpired: true);

        int corruptionLevel;
        if (cliquesManager.IsCliqueActive("WholesaleTrader"))
        {
            corruptionLevel = 0;
        }
        else
        {
            List<(int corruptionLevel, float chance)> corruption = [(0, 0.6f), (1, 0.3f), (2, 0.1f)];
            if (onMilitarySupervision)
            {
                corruption[0] = (0, corruption[0].chance - 0.3f);
                corruption[1] = (1, corruption[1].chance + 0.2f);
                corruption[2] = (2, corruption[2].chance + 0.1f);
            }
            if (HasDisorderlyMilitaryTag)
            {
                corruption[0] = (0, corruption[0].chance - 0.1f);
                corruption[1] = (1, corruption[1].chance + 0.05f);
                corruption[2] = (2, corruption[2].chance + 0.05f);
            }
            if (HasQuestEffectTag("Integrity"))
            {
                corruption[0] = (0, corruption[0].chance - 0.2f);
                corruption[1] = (1, corruption[1].chance - 0.1f);
                corruption[2] = (2, corruption[2].chance - 0.1f);
            }
            corruptionLevel = corruption.RandomElementByWeight(t => t.chance).corruptionLevel;
        }

        TaggedString text;
        LetterDef letterDef;
        if (corruptionLevel <= 0)
        {
            FamineRisk -= 0.75f;
            letterDef = LetterDefOf.PositiveEvent;
            text = $"OARO_RefugeeInflux_GrainArrivalText_{corruptionLevel}".Translate(Name, 0.75f.ToStringPercent("F2"));
        }
        else if (corruptionLevel == 1)
        {
            FamineRisk -= 0.55f;
            AdjuestRefugeeWillingness(-0.05f);
            letterDef = LetterDefOf.NeutralEvent;
            text = $"OARO_RefugeeInflux_GrainArrivalText_{corruptionLevel}".Translate(Name, 0.55f.ToStringPercent("F2"), 0.05f.ToStringPercent("f2"));
        }
        else
        {
            FamineRisk -= 0.25f;
            AdjuestRefugeeWillingness(-0.2f);
            letterDef = LetterDefOf.NegativeEvent;
            text = $"OARO_RefugeeInflux_GrainArrivalText_{corruptionLevel}".Translate(Name, 0.25f.ToStringPercent("F2"), 0.2f.ToStringPercent("f2"));
        }

        Find.LetterStack.ReceiveLetter("OARO_RefugeeInflux_GrainArrivalLabel".Translate(Name), text, letterDef, lookTargets: this, quest: quest);
    }

    private void RecacheExtraFixeddistEfficiency()
    {
        extraFixeddistEfficiency = 0f;
        forceDist = !cooldownManager.IsInCooldown("ForceDist");

        if (curPolicy == PolicyType.ImproveDist)
        {
            extraFixeddistEfficiency += (0.05f + TotalPotency * 0.2f);
        }
        if (onMilitarySupervision)
        {
            extraFixeddistEfficiency += HasDisorderlyMilitaryTag ? 0.1f : 0.2f;
        }
        else
        {
            extraFixeddistEfficiency += (cliquesManager.GetCliqueWillingness(RefugeeCliqueKey) * 0.2f);
        }
        if (cliquesManager.IsCliqueActive("NearbyTown"))
        {
            extraFixeddistEfficiency += 0.1f;
        }
    }

    protected override void FinishWork()
    {
        switch (curWork)
        {
            case WorkType.Hunting:
                {
                    int maxAnimalsLevel = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Animals);
                    int totalShootingLevel = OARO_PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Shooting);
                    float famineRiskChange = (maxAnimalsLevel * 0.002f + totalShootingLevel * 0.0001f) * Rand.Range(0.5f, 1.5f);
                    if (HasQuestEffectTag("HuntingGround"))
                    {
                        famineRiskChange *= 1.5f;
                    }
                    FamineRisk -= famineRiskChange;
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_RefugeeInflux_HuntingResult".Translate(famineRiskChange.ToStringPercent("F2"))));
                    break;
                }
            case WorkType.AssistPacify:
                {
                    float willingnessChange = OARO_PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Social) * 0.001f;
                    AdjuestRefugeeWillingness(willingnessChange);
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_RefugeeInflux_PacifyResult".Translate(willingnessChange.ToStringPercent("F2"))));
                    break;
                }
            default: break;
        }
    }

    protected override void InterruptWork() { }

    private void AdjustPopulation(int change)
    {
        population -= change;
        yestFamineRiskChange = change;
        if (population < 300)
        {
            this.SafeDestroy();
        }
    }

    private void ExileRefugees()
    {
        cooldownManager.RegisterRecord("ForceDist", cdTicks: 5 * 60000, shouldRemoveWhenExpired: true);
        forceDist = true;
        AdjuestRefugeeWillingness(-0.25f);
        AdjustPopulation(-100);

        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_RefugeeInflux_ExileResult".Translate(100, 0.25f.ToStringPercent("F2"))));
    }

    private void MilitaryControl()
    {
        onMilitarySupervision = true;
        CliquesManager.RemoveClique(RefugeeCliqueKey);
        CliquesManager.TryActiveClique(RoyalArmyCliqueKey, directly: true);
        if (isWorking)
        {
            EndWork(interrupt: true);
        }
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_RefugeeInflux_MilitarySupervisionResult".Translate()));
    }

    private DiaNode DistributionNode(Caravan caravan)
    {
        DiaNode diaNode = new("OARO_RefugeeInflux_DistributionFoodInfo".Translate());

        DiaOption caravanOpt = new("OARO_RefugeeInflux_DistributionFood_Caravan".Translate())
        {

            resolveTree = true
        };
        diaNode.options.Add(caravanOpt);

        DiaOption branchOpt = new("OARO_RefugeeInflux_DistributionFood_Branch".Translate())
        {
            action = delegate
            {
                branch.Squad.SquadStat.Supply -= 0.5f;
                Distribute();
            },
            resolveTree = true
        };
        if (branch.Squad.SquadStat.Supply < 0.5f)
        {
            branchOpt.Disable("OARO_Insufficient_SquadSupply".Translate(0.5f.ToStringPercent()));
        }
        diaNode.options.Add(branchOpt);

        DiaOption townOpt = new("OARO_RefugeeInflux_DistributionFood_Town".Translate())
        {
            action = Distribute,
            resolveTree = true
        };
        if (!CliquesManager.IsCliqueActive("NearbyTown"))
        {
            townOpt.Disable("OARO_Disable_CliqueInactive".Translate());
        }
        diaNode.options.Add(townOpt);

        DiaOption backOpt = new("GoBack".Translate())
        {
            linkLateBind = () => ArrivedDiaNode(caravan)
        };
        diaNode.options.Add(backOpt);

        return diaNode;

        void Distribute()
        {
            cooldownManager.RegisterRecord("DistributionFood", cdTicks: 5 * 60000, shouldRemoveWhenExpired: true);
            FamineRisk -= 0.55f;
            AdjuestRefugeeWillingness(0.2f);
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_RefugeeInflux_DistributionFoodResult".Translate(0.55f.ToStringPercent("F2"), 0.2f.ToStringPercent("F2"))));
        }
    }

    private void AdjuestRefugeeWillingness(float change, bool showMessage = true)
    {
        if (!onMilitarySupervision)
        {
            CliquesManager.AdjustCliqueWillingness(RefugeeCliqueKey, change, showMessage);
        }
    }

    public override void Destroy()
    {
        if (!onMilitarySupervision)
        {
            QuestUtility.SendQuestTargetSignals(questTags, "PopulationSettled", this.Named("SUBJECT"), population.Named("POPULATION"));
        }
        base.Destroy();
    }
}
