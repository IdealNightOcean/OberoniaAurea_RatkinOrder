using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_RefugeeInflux : WorldObject_BranchDemand
{
    private bool HasDisorderlyMilitaryTag => HasQuestEffectTag("DisorderlyMilitary");
    public string RefugeeCliqueKey => "Refugee_" + ID;
    public string RoyalArmyCliqueKey => "RoyalArmy_" + ID;

    public override int TicksNeeded => 30000;

    private bool onMilitarySupervision;

    private int branchPolicy; // 0: 安抚难民，1: 改善配给
    private int workType; // 0: 就近狩猎，1: 安抚难民

    private int population;
    private float lastPopulationChange;

    private float distEfficiency;
    private float extraFixeddistEfficiency;
    private float lastDistEfficiencyChange;
    private bool forceDist;

    private float famineRisk;
    private float lastFamineRiskChange;

    private int ticksToNextCheck;

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
            lastDistEfficiencyChange += (distEfficiency - oldDistEfficiency);
        }
    }
    private float FamineRisk
    {
        get => famineRisk;
        set
        {
            float oldFamineRisk = famineRisk;
            famineRisk = Mathf.Clamp01(value);
            lastFamineRiskChange += (famineRisk - oldFamineRisk);
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

        Scribe_Values.Look(ref branchPolicy, "branchPolicy", 0);
        Scribe_Values.Look(ref workType, "workType", 0);

        Scribe_Values.Look(ref population, "population", 0);
        Scribe_Values.Look(ref lastPopulationChange, "lastPopulationChange", 0f);

        Scribe_Values.Look(ref distEfficiency, "distEfficiency", 0f);
        Scribe_Values.Look(ref extraFixeddistEfficiency, "extraFixeddistEfficiency", 0f);
        Scribe_Values.Look(ref lastDistEfficiencyChange, "lastDistEfficiencyChange", 0f);
        Scribe_Values.Look(ref forceDist, "forceDist", defaultValue: false);

        Scribe_Values.Look(ref famineRisk, "famineRisk", 0f);
        Scribe_Values.Look(ref lastFamineRiskChange, "lastFamineRiskChange", 0f);

        Scribe_Values.Look(ref ticksToNextCheck, "ticksToNextCheck", 0);

        Scribe_Deep.Look(ref cooldownManager, "cooldownManager", 0);
    }

    public override void PostAdd()
    {
        base.PostAdd();
        population = Rand.RangeInclusive(1000, 1500);
        distEfficiency = 0.5f;
        famineRisk = 0f;
        ticksToNextCheck = 2500;
        cooldownManager.RegisterRecord("PeriodicCheck", cdTicks: 30000, shouldRemoveWhenExpired: true);
        cooldownManager.RegisterRecord("GrainArrival", cdTicks: 5 * 30000, shouldRemoveWhenExpired: true);

        QuestClique refugeeClique = new()
        {
            Name = "OARO_Name_RefugeeClique".Translate(Name),
            ActiveDesc = "OARO_ActiveDesc_RefugeeClique".Translate(),
            InactiveDesc = "OARO_InactiveDesc_RefugeeClique".Translate(),
            Potency = 0.2f,
            Willingness = Rand.Range(0.15f, 0.25f),
            IsActivatable = true,
            IsCommunicable = true,
            IsBribable = false,

            PreferredBuilding = BranchBuildingDefOf.OARO_Church
        };

        QuestClique royalArmyClique = new()
        {
            Name = "OARO_Name_RoyalArmyClique".Translate(Name),
            ActiveDesc = "OARO_ActiveDesc_RoyalArmyClique".Translate(),
            InactiveDesc = "OARO_InactiveDesc_RoyalArmyClique".Translate(),
            Potency = -0.35f,
            IsActivatable = true,
            IsCommunicable = false,
            IsBribable = false,
        };

        if (HasDisorderlyMilitaryTag)
        {
            royalArmyClique.Potency = -0.5f;
        }

        CliquesManager.TryAddClique(RefugeeCliqueKey, refugeeClique);
        CliquesManager.TryAddClique(RefugeeCliqueKey, royalArmyClique);
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
                        workType = 0;
                        base.Notify_CaravanArrived(caravan);
                    },
                    resolveTree = true
                };
                rootNode.options.Add(huntingOpt);

                DiaOption pacifyOpt = new("OARO_RefugeeInflux_Pacify".Translate())
                {
                    action = delegate
                    {
                        workType = 1;
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

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (!Destroyed && (ticksToNextCheck -= delta) <= 0)
        {
            ticksToNextCheck = 2500;
            if (!cooldownManager.IsInCooldown("PeriodicCheck"))
            {
                PeriodicCheck(60000);
            }
        }
    }

    /// <summary>
    /// 60000Tick (24小时)一周期
    /// </summary>
    private void PeriodicCheck(int nextCheckDelay)
    {
        if (cooldownManager.IsInCooldown("PeriodicCheck"))
        {
            return;
        }

        cooldownManager.RegisterRecord("PeriodicCheck", cdTicks: nextCheckDelay, shouldRemoveWhenExpired: true);
        FrameCheck();
        if (Destroyed)
        {
            return;
        }

        lastDistEfficiencyChange = 0f;
        lastFamineRiskChange = 0f;
        lastPopulationChange = 0f;

        RecacheExtraFixeddistEfficiency();

        float famineRiskChange = Rand.Range(0.08f, 0.16f);

        if (cooldownManager.IsInCooldown("GrainArrival"))
        {
            cooldownManager.RegisterRecord("GrainArrival", cdTicks: 5 * 60000, shouldRemoveWhenExpired: true);
            TaggedString label;
            TaggedString text;
            LetterDef letterDef = LetterDefOf.NeutralEvent;
            float chance = Rand.Value;

            if (chance < 0.6f || CliquesManager.IsCliqueActive("WholesaleTrader"))
            {
                famineRiskChange -= 0.75f;
            }
            else if (chance < 0.9f)
            {
                famineRiskChange -= 0.55f;
                AdjuestRefugeeWillingness(-0.05f);
            }
            else
            {
                famineRiskChange -= 0.25f;
                AdjuestRefugeeWillingness(-0.2f);
            }
            Find.LetterStack.ReceiveLetter(label, text, letterDef, lookTargets: this, quest: quest);
        }

        famineRiskChange += (1f - DistEfficiency);
        FamineRisk += famineRiskChange;
    }

    private void FrameCheck()
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

    private void RecacheExtraFixeddistEfficiency()
    {
        extraFixeddistEfficiency = 0f;
        forceDist = !cooldownManager.IsInCooldown("ForceDist");

        if (branchPolicy == 1)
        {
            extraFixeddistEfficiency += (0.05f + TotalPotency * 0.2f);
        }
        if (onMilitarySupervision)
        {
            extraFixeddistEfficiency += 0.2f;
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
        switch (workType)
        {
            case 0:
                {
                    int maxAnimalsLevel = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Animals);
                    int totalShootingLevel = OARO_PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Shooting);
                    float famineRiskChange = maxAnimalsLevel * 0.002f + totalShootingLevel * 0.0001f * Rand.Range(0.5f, 1.5f);
                    FamineRisk -= famineRiskChange;
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_RefugeeInflux_HuntingResult".Translate(famineRiskChange.ToStringPercent("F2"))));
                    break;
                }
            case 1:
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
        lastFamineRiskChange = change;
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

    private void AdjuestRefugeeWillingness(float change)
    {
        if (!onMilitarySupervision)
        {
            CliquesManager.AdjustCliqueWillingness(RefugeeCliqueKey, change);
        }
    }
}
