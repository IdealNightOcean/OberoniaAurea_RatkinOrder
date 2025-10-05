using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Utility;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_PlagueVillage : WorldObject_CriticalBranchDemand
{
    private enum PolicyType : byte
    {
        OrderControl,
        SampleCollection,
        SurveyAssistance
    }
    private enum WorkType : byte
    {
        Cure,
        Isolation
    }

    //多次使用的 QuestEffectTag
    private const string ResponsibleDoctor = "ResponsibleDoctor"; //尽责医生
    private const string MedicalInfusion = "MedicalInfusion"; //医疗充盈
    private bool HasStrangePlagueTag => HasQuestEffectTag("StrangePlague");

    public override int TicksNeeded => 30000;
    protected override int PeriodicCheckInterval => 15000;

    private PolicyType curPolicy;
    private WorkType curWork;

    private int nextPlagueSpreadTick = -1;
    private int nextCanProvideMedicineTick = -1;

    private int originalPopulation;
    private int population;

    private float plagueSpread;
    private float PlagueSpread
    {
        get => plagueSpread;
        set => plagueSpread = Mathf.Clamp(value, 0f, 2f);
    }

    private float maxPlagueControl = 600f;
    private float plagueControl;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref curPolicy, "curPolicy");
        Scribe_Values.Look(ref curWork, "curWork");

        Scribe_Values.Look(ref nextPlagueSpreadTick, "nextPlagueSpreadTick", -1);

        Scribe_Values.Look(ref originalPopulation, "originalPopulation", 0);
        Scribe_Values.Look(ref population, "population", 0);

        Scribe_Values.Look(ref plagueSpread, "plagueSpread", 0f);

        Scribe_Values.Look(ref maxPlagueControl, "maxPlagueControl", 600f);
        Scribe_Values.Look(ref plagueControl, "plagueControl", 0f);
    }

    public override void PostAdd()
    {
        originalPopulation = Rand.RangeInclusive(3000, 4000);
        population = originalPopulation;

        PlagueSpread = Rand.Range(0.3f, 0.45f);
        plagueControl = 0f;

        nextPlagueSpreadTick = Find.TickManager.TicksGame + 60000;

        if (EffectTags is not null)
        {
            if (effectTags.HasTag("Panic"))
            {
                PlagueSpread += 0.1f;
            }

            if (effectTags.HasTag(ResponsibleDoctor))
            {
                CliquesManager.AdjustCliqueWillingness(KeyLibrary_QuestCliqueKey.Doctor, 0.1f);
                CliquesManager.AdjustCliquePotency(KeyLibrary_QuestCliqueKey.Doctor, 0.15f);
            }

            if (effectTags.HasTag("LargeTown"))
            {
                originalPopulation = Rand.RangeInclusive(7000, 8000);
                population = originalPopulation;
                PlagueSpread += 0.1f;
                maxPlagueControl += 150;
            }

            if (effectTags.HasTag(MedicalInfusion))
            {
                PlagueSpread -= 0.05f;
            }
        }
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine("OARO_WorldObejct_CurPolicy".Translate());
        sb.Append(": ");
        sb.Append($"OARO_PlagueVillage_{curPolicy}".Translate());

        if (isWorking)
        {
            sb.AppendInNewLine("OARO_WorldObejct_CurWork".Translate());
            sb.Append(": ");
            sb.Append($"OARO_PlagueVillage_{curWork}".Translate());
        }

        Color PopulationColor = population switch
        {
            > 1000 => Color.white,
            > 800 => Color.yellow,
            > 600 => ColorLibrary.Orange,
            _ => ColorLibrary.RedReadable
        };
        sb.AppendInNewLine("OARO_WorldObejct_PopulationInfo".Translate(population, originalPopulation).Colorize(PopulationColor));
        sb.AppendInNewLine("OARO_PlagueVillage_Control".Translate(plagueControl.ToString("F2"), maxPlagueControl.ToString("F2")));

        Color SpreadColor = plagueSpread switch
        {
            < 0.5f => Color.green,
            < 1f => Color.white,
            < 1.5f => Color.yellow,
            < 1.8f => ColorLibrary.Orange,
            _ => ColorLibrary.RedReadable
        };
        sb.AppendInNewLine("OARO_PlagueVillage_PlagueSpread".Translate(plagueSpread.ToStringPercent("F2")).Colorize(SpreadColor));

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
            defaultLabel = "DEV: Add 100 Control",
            action = delegate
            {
                AdjustPlagueControl(100);
            }
        };
        yield return new Command_Action()
        {
            defaultLabel = "DEV: Add 20% Spread",
            action = delegate
            {
                PlagueSpread += 0.2f;
            }
        };
        yield return new Command_Action()
        {
            defaultLabel = "DEV: Reduce 20% Spread",
            action = delegate
            {
                PlagueSpread -= 0.2f;
            }
        };
        yield return new Command_Action()
        {
            defaultLabel = "DEV: Reduce 500 Population",
            action = delegate
            {
                AdjustPopulation(-500);
            }
        };
        yield return new Command_Action()
        {
            defaultLabel = "DEV: Reduce 100 Population",
            action = delegate
            {
                AdjustPopulation(-100);
            }
        };
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        Find.WindowStack.Add(new Dialog_NodeTree(ArrivedDiaNode(caravan)));
    }

    public void AdjustPlagueControl(float change)
    {
        plagueControl = Mathf.Clamp(plagueControl + change, 0f, maxPlagueControl);
        if (plagueControl >= maxPlagueControl)
        {
            PlagueResolved();
        }
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is not null)
        {
            switch (curWork)
            {
                case WorkType.Cure:
                    {
                        int maxMedicineLevel = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Medicine);
                        int totalMedicineLevel = OARO_PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Medicine);
                        float controlAdd = maxMedicineLevel + totalMedicineLevel * 0.2f;
                        if (HasStrangePlagueTag)
                        {
                            controlAdd *= 0.5f;
                        }
                        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree($"OARO_PlagueVillage_{curWork}Result".Translate(controlAdd.ToString("F2"))));
                        AdjustPlagueControl(controlAdd);
                        return;
                    }
                case WorkType.Isolation:
                    {
                        int totalSocialLevel = OARO_PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Social);
                        float spreadReduce = totalSocialLevel * 0.00025f;
                        PlagueSpread -= spreadReduce;
                        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree($"OARO_PlagueVillage_{curWork}Result".Translate(spreadReduce.ToStringPercent("F2"))));
                        return;
                    }
                default: return;
            }
        }
    }

    protected override void InterruptWork() { }

    private DiaNode ArrivedDiaNode(Caravan caravan)
    {
        DiaNode rootNode = new("OARO_PlagueVillage_ArrivalInfo".Translate());

        DiaOption dispensingOpt = new("OARO_PlagueVillage_Dispatch".Translate())
        {
            linkLateBind = () => DispatchNode(caravan),
            resolveTree = false
        };
        rootNode.options.Add(dispensingOpt);

        DiaOption sellOpt = new("OARO_PlagueVillage_Sell".Translate())
        {
            linkLateBind = () => SellNode(caravan),
            resolveTree = false
        };
        rootNode.options.Add(sellOpt);

        if (Find.TickManager.TicksGame < nextCanProvideMedicineTick)
        {
            int cooldownTicksLeft = nextCanProvideMedicineTick - Find.TickManager.TicksGame;
            dispensingOpt.Disable("WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod()));
            sellOpt.Disable("WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod()));
        }

        if (!isWorking)
        {
            foreach (WorkType workType in EnumUtility.GetValues<WorkType>())
            {
                TaggedString optLabel = $"OARO_PlagueVillage_{workType}".Translate() + " (" + $"OARO_PlagueVillage_{workType}_Skill".Translate() + ")";
                DiaOption workOpt = new(optLabel)
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

        rootNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);
        return rootNode;
    }

    private DiaNode DispatchNode(Caravan caravan)
    {
        DiaNode dispatchNode = new("OARO_PlagueVillage_DispatchInfo".Translate());

        DiaOption herbalOpt = new($"{ThingDefOf.MedicineHerbal.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineHerbal, Rand.Range(0.02f, 0.03f)),
            resolveTree = true
        };
        if (CaravanInventoryUtility.HasThings(caravan, ThingDefOf.MedicineHerbal, 25))
        {
            herbalOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.MedicineHerbal.label, 25));
        }
        dispatchNode.options.Add(herbalOpt);

        DiaOption industrialOpt = new($"{ThingDefOf.MedicineIndustrial.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineIndustrial, Rand.Range(0.035f, 0.05f)),
            resolveTree = true
        };
        if (CaravanInventoryUtility.HasThings(caravan, ThingDefOf.MedicineIndustrial, 25))
        {
            industrialOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.MedicineIndustrial.label, 25));
        }
        dispatchNode.options.Add(industrialOpt);

        DiaOption utratechOpt = new($"{ThingDefOf.MedicineUltratech.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineUltratech, Rand.Range(0.1f, 0.16f)),
            resolveTree = true
        };
        if (CaravanInventoryUtility.HasThings(caravan, ThingDefOf.MedicineUltratech, 25))
        {
            utratechOpt.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.MedicineUltratech.label, 25));
        }
        dispatchNode.options.Add(utratechOpt);

        return dispatchNode;

        void DispatchResult(ThingDef thingDef, float spreadChange)
        {
            nextCanProvideMedicineTick = Find.TickManager.TicksGame + 120000;
            if (HasQuestEffectTag(ResponsibleDoctor))
            {
                spreadChange *= 1.25f;
            }
            PlagueSpread -= spreadChange;
            caravan.RemoveThingsOfDef(thingDef, 25);
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PlagueVillage_DispatchResult".Translate(spreadChange.ToString("F2"))));
        }
    }

    private DiaNode SellNode(Caravan caravan)
    {
        DiaNode sellNode = new("OARO_PlagueVillage_SellInfo".Translate());

        DiaOption herbalOpt = new($"{ThingDefOf.MedicineHerbal.label} x 25")
        {
            action = () => SellResult(ThingDefOf.MedicineHerbal, Rand.Range(0.015f, 0.02f)),
            resolveTree = true
        };
        sellNode.options.Add(herbalOpt);

        DiaOption industrialOpt = new($"{ThingDefOf.MedicineIndustrial.label} x 25")
        {
            action = () => SellResult(ThingDefOf.MedicineIndustrial, Rand.Range(0.025f, 0.03f)),
            resolveTree = true
        };
        sellNode.options.Add(industrialOpt);

        DiaOption utratechOpt = new($"{ThingDefOf.MedicineUltratech.label} x 25")
        {
            action = () => SellResult(ThingDefOf.MedicineUltratech, Rand.Range(0.05f, 0.08f)),
            resolveTree = true
        };
        sellNode.options.Add(utratechOpt);

        return sellNode;

        void SellResult(ThingDef thingDef, float spreadChange)
        {
            nextCanProvideMedicineTick = Find.TickManager.TicksGame + 120000;
            PlagueSpread -= spreadChange;
            caravan.RemoveThingsOfDef(thingDef, 25);
            int silverGain = (int)thingDef.GetStatValueAbstract(StatDefOf.MarketValue) * 25 * 4;
            List<Thing> silverList = OAFrame_MiscUtility.TryGenerateThing(ThingDefOf.Silver, silverGain);
            foreach (Thing thing in silverList)
            {
                CaravanInventoryUtility.GiveThing(caravan, thing);
            }

            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PlagueVillage_SellResult".Translate(spreadChange.ToString("F2"), silverGain)));
        }
    }

    protected override void PeriodicCheck()
    {
        switch (curPolicy)
        {
            case PolicyType.OrderControl:
                {
                    float plagueSpreadReduce = 0.002f + TotalPotency * 0.01f;
                    plagueSpread -= plagueSpreadReduce;

                    float controlAdd = 2 + TotalPotency * 6f;
                    if (HasStrangePlagueTag)
                    {
                        controlAdd *= 0.5f;
                    }
                    Messages.Message("OARO_PlagueVillage_OrderControlResult".Translate(plagueSpreadReduce.ToStringPercent("F2"), controlAdd.ToString("F2")), MessageTypeDefOf.PositiveEvent, historical: true);
                    AdjustPlagueControl(controlAdd);
                    break;
                }
            case PolicyType.SampleCollection:
                {
                    if (Rand.Chance(TotalPotency * 0.6f))
                    {
                        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
                        if (map is not null)
                        {
                            Thing thing = ThingMaker.MakeThing(OARO_ThingDefOf.OARO_PlagueSample);
                            thing.TryGetComp<CompPlagueSample>()?.InitSample(quest, this, HasStrangePlagueTag);
                            OAFrame_DropPodUtility.DefaultDropSingleThing(thing, map, branch?.RatkinOrder?.Faction, sendLetter: false);

                            Find.LetterStack.ReceiveLetter(
                                label: "OARO_PlagueVillage_SampleCollectionLabel".Translate(),
                                text: "OARO_PlagueVillage_SampleCollectionText".Translate(),
                                textLetterDef: LetterDefOf.PositiveEvent,
                                lookTargets: thing,
                                relatedFaction: branch?.RatkinOrder.Faction,
                                quest: quest);
                        }
                    }
                    break;
                }
            case PolicyType.SurveyAssistance:
                {
                    float controlAdd = 2 + TotalPotency * 12f;
                    if (HasStrangePlagueTag)
                    {
                        controlAdd *= 0.5f;
                    }
                    Messages.Message("OARO_PlagueVillage_SurveyAssistanceResult".Translate(controlAdd.ToString("F2")), MessageTypeDefOf.PositiveEvent, historical: true);
                    AdjustPlagueControl(controlAdd);
                    break;
                }
            default:
                break;
        }

        if (!Destroyed && Find.TickManager.TicksGame >= nextPlagueSpreadTick)
        {
            nextPlagueSpreadTick = Find.TickManager.TicksGame + 60000;
            DailySpreadPlague();
        }
    }

    private void DailySpreadPlague()
    {
        float spreadCount = 15f + (population * PlagueSpread * 0.1f);
        if (HasQuestEffectTag(MedicalInfusion))
        {
            spreadCount *= 0.8f;
        }

        AdjustPopulation(Mathf.FloorToInt(spreadCount));

        if (Destroyed)
        {
            return;
        }

        if (plagueSpread <= 0.1f && Rand.Chance(0.1f))
        {
            PlagueResolved();
            return;
        }

        if (CliquesManager is not null)
        {
            if (plagueSpread > 0.5f)
            {
                cliquesManager.AdjustCliqueWillingness(KeyLibrary_QuestCliqueKey.Civilian, -0.1f * plagueSpread);
            }
            if (cliquesManager.HasClique(KeyLibrary_QuestCliqueKey.PanickedCivilian))
            {
                if (plagueSpread < 0.2f)
                {
                    cliquesManager.RemoveClique(KeyLibrary_QuestCliqueKey.PanickedCivilian);
                }
                else
                {
                    int plagueImpact = Mathf.FloorToInt(15 * plagueSpread);
                    Messages.Message("OARO_PlagueVillage_PanickedPlagueImpact".Translate(plagueImpact), MessageTypeDefOf.NegativeEvent, historical: true);
                    AdjustPlagueControl(-plagueImpact);
                }
            }
        }

        PlagueSpread += (0.02f + PlagueSpread * 0.05f);
    }

    private void AdjustPopulation(int change)
    {
        population = Math.Max(0, population - change);
        if (population < 500)
        {
            this.SafeDestroy();
        }
    }

    private void PlagueResolved()
    {
        SendWorkResolvedSignal([population.Named("POPULATION")]);
        this.SafeDestroy();
    }
}