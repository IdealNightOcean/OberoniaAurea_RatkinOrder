using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_PlagueVillage : WorldObject_InteractWithFixedCaravan_Nameable, ISingleBranchRelated
{
    public override int TicksNeeded => 30000;

    private int branchPolicy; // 1:秩序管控，2:样本收集，3:调查协助
    private int workType; // 1:协助救治，2:协助隔离

    [Unsaved] private bool hasSettled;

    private int nextPlagueSpreadTick = -1;
    private int nextPeriodicCheckTick = -1;

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

    private Branch branch;
    public Branch Branch => branch;

    [Unsaved] QuestPart_EffectTags effectTags;
    public QuestPart_EffectTags EffectTags => effectTags ?? (QuestPart_EffectTags.TryGetEffectTags(quest, addPartIfMiss: false, out effectTags) ? effectTags : null);

    [Unsaved] QuestPart_CliquesManager cliquesManager;
    public QuestPart_CliquesManager CliquesManager => cliquesManager ?? (QuestPart_CliquesManager.TryGetCliquesManager(quest, addPartIfMiss: false, out cliquesManager) ? cliquesManager : null);
    private float TotalPotency => CliquesManager?.TotalPotency ?? 0f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref branchPolicy, "branchPolicy", 0);
        Scribe_Values.Look(ref workType, "workType", 0);

        Scribe_Values.Look(ref nextPlagueSpreadTick, "nextPlagueSpreadTick", -1);
        Scribe_Values.Look(ref nextPeriodicCheckTick, "nextPeriodicCheckTick", -1);

        Scribe_Values.Look(ref originalPopulation, "originalPopulation", 0);
        Scribe_Values.Look(ref population, "population", 0);

        Scribe_Values.Look(ref plagueSpread, "plagueSpread", 0f);

        Scribe_Values.Look(ref maxPlagueControl, "maxPlagueControl", 600f);
        Scribe_Values.Look(ref plagueControl, "plagueControl", 0f);

        Scribe_References.Look(ref branch, "branch");
    }

    public override void PostAdd()
    {
        originalPopulation = Rand.RangeInclusive(3000, 4000);
        population = originalPopulation;

        PlagueSpread = Rand.Range(0.3f, 0.45f);
        plagueControl = 0f;

        nextPeriodicCheckTick = Find.TickManager.TicksGame + 15000;
        nextPlagueSpreadTick = Find.TickManager.TicksGame + 60000;

        if (EffectTags is not null)
        {
            if (effectTags.HasTag(KeyLibrary_QuestEffectTag.Panic))
            {
                PlagueSpread += 0.1f;
            }

            if (effectTags.HasTag(KeyLibrary_QuestEffectTag.ResponsibleDoctor))
            {
                CliquesManager?.AdjustCliqueWillingness(KeyLibrary_QuestCliqueKey.Doctor, 0.1f);
                CliquesManager?.AdjustCliquePotency(KeyLibrary_QuestCliqueKey.Doctor, 0.15f);
            }

            if (effectTags.HasTag(KeyLibrary_QuestEffectTag.LargeTown))
            {
                originalPopulation = Rand.RangeInclusive(7000, 8000);
                population = originalPopulation;
                PlagueSpread += 0.1f;
                maxPlagueControl += 150;
            }

            if (effectTags.HasTag(KeyLibrary_QuestEffectTag.MedicalInfusion))
            {
                PlagueSpread -= 0.05f;
            }
        }
    }

    public void InitOrderBranch(Branch branch)
    {
        this.branch = branch;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            PlagueOutOfControl();
        }
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        if (this.branch == branch)
        {
            PlagueOutOfControl();
        }
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        OpenStartDialog(caravan);
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
            switch (workType)
            {
                case 1:
                    {
                        int maxMedicineLevel = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Medicine);
                        int totalMedicineLevel = PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Medicine);
                        float controlAdd = maxMedicineLevel + totalMedicineLevel * 0.2f;
                        if (EffectTags?.HasTag(KeyLibrary_QuestEffectTag.StrangePlague) ?? false)
                        {
                            controlAdd *= 0.5f;
                        }
                        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PlagueVillage_CureResult".Translate(controlAdd.ToString("F2"))));
                        AdjustPlagueControl(controlAdd);
                        return;
                    }
                case 2:
                    {
                        int totalSocialLevel = PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Social);
                        float spreadReduce = totalSocialLevel * 0.00025f;
                        PlagueSpread -= spreadReduce;
                        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PlagueVillage_IsolationResult".Translate(spreadReduce.ToStringPercent("F2"))));
                        return;
                    }
                default: return;
            }
        }
    }

    protected override void InterruptWork() { }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (!hasSettled && Find.TickManager.TicksGame >= nextPeriodicCheckTick)
        {
            nextPeriodicCheckTick = Find.TickManager.TicksGame + 15000;
            PeriodicCheck();

            if (!hasSettled && Find.TickManager.TicksGame >= nextPlagueSpreadTick)
            {
                nextPlagueSpreadTick = Find.TickManager.TicksGame + 60000;
                DailySpreadPlague();
            }
        }
    }

    private void OpenStartDialog(Caravan caravan)
    {
        DiaNode rootNode = new("OARO_PlagueVillage_StartInfo".Translate());

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

        DiaOption cureOpt = new("OARO_PlagueVillage_Cure".Translate())
        {
            action = delegate
            {
                workType = 1;
                base.StartWork(caravan);
            },
            resolveTree = true
        };
        rootNode.options.Add(cureOpt);

        DiaOption isolationOpt = new("OARO_PlagueVillage_Isolation".Translate())
        {
            action = delegate
            {
                workType = 2;
                base.StartWork(caravan);
            },
            resolveTree = true
        };
        rootNode.options.Add(isolationOpt);

        Dialog_NodeTree dialog = new(rootNode);
        Find.WindowStack.Add(dialog);
    }

    private DiaNode DispatchNode(Caravan caravan)
    {
        DiaNode dispatchNode = new("OARO_PlagueVillage_DispatchInfo".Translate());

        DiaOption herbalOpt = new($"{ThingDefOf.MedicineHerbal.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineHerbal, Rand.Range(0.02f, 0.03f)),
            resolveTree = true
        };
        dispatchNode.options.Add(herbalOpt);

        DiaOption industrialOpt = new($"{ThingDefOf.MedicineIndustrial.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineIndustrial, Rand.Range(0.035f, 0.05f)),
            resolveTree = true
        };
        dispatchNode.options.Add(industrialOpt);

        DiaOption utratechOpt = new($"{ThingDefOf.MedicineUltratech.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineUltratech, Rand.Range(0.1f, 0.16f)),
            resolveTree = true
        };
        dispatchNode.options.Add(utratechOpt);

        return dispatchNode;

        void DispatchResult(ThingDef thingDef, float spreadChange)
        {
            if (EffectTags?.HasTag(KeyLibrary_QuestEffectTag.ResponsibleDoctor) ?? false)
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
        DiaNode dispatchNode = new("OARO_PlagueVillage_SellInfo".Translate());

        DiaOption herbalOpt = new($"{ThingDefOf.MedicineHerbal.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineHerbal, Rand.Range(0.015f, 0.02f)),
            resolveTree = true
        };
        dispatchNode.options.Add(herbalOpt);

        DiaOption industrialOpt = new($"{ThingDefOf.MedicineIndustrial.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineIndustrial, Rand.Range(0.025f, 0.03f)),
            resolveTree = true
        };
        dispatchNode.options.Add(industrialOpt);

        DiaOption utratechOpt = new($"{ThingDefOf.MedicineUltratech.label} x 25")
        {
            action = () => DispatchResult(ThingDefOf.MedicineUltratech, Rand.Range(0.05f, 0.08f)),
            resolveTree = true
        };
        dispatchNode.options.Add(utratechOpt);

        return dispatchNode;

        void DispatchResult(ThingDef thingDef, float spreadChange)
        {
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

    private void PeriodicCheck()
    {
        switch (branchPolicy)
        {
            case 1:
                {
                    float plagueSpreadReduce = 0.002f + TotalPotency * 0.01f;
                    plagueSpread -= plagueSpreadReduce;

                    float controlAdd = 2 + TotalPotency * 6f;
                    if (EffectTags?.HasTag(KeyLibrary_QuestEffectTag.StrangePlague) ?? false)
                    {
                        controlAdd *= 0.5f;
                    }
                    Messages.Message("OARO_PlagueVillage_OrderControlResult".Translate(plagueSpreadReduce.ToStringPercent("F2"), controlAdd.ToString("F2")), MessageTypeDefOf.PositiveEvent, historical: true);
                    AdjustPlagueControl(controlAdd);
                    break;
                }
            case 2:
                {
                    if (Rand.Chance(TotalPotency * 0.6f))
                    {
                        Map map = MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
                        if (map is not null)
                        {
                            Thing thing = ThingMaker.MakeThing(OARO_ThingDefOf.OARO_PlagueSample);
                            thing.TryGetComp<CompPlagueSample>()?.InitSample(quest, this, EffectTags?.HasTag(KeyLibrary_QuestEffectTag.StrangePlague) ?? false);
                            OAFrame_DropPodUtility.DefaultDropSingleThing(thing, map, branch?.RatkinOrder?.Faction, sendLetter: false);

                        }
                    }
                    break;
                }
            case 3:
                {
                    float controlAdd = 2 + TotalPotency * 12f;
                    if (EffectTags?.HasTag(KeyLibrary_QuestEffectTag.StrangePlague) ?? false)
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

        if (!hasSettled && Find.TickManager.TicksGame >= nextPeriodicCheckTick)
        {
            nextPlagueSpreadTick = Find.TickManager.TicksGame + 60000;
            DailySpreadPlague();
        }
    }

    private void DailySpreadPlague()
    {
        float spreadCount = 15f + (population * PlagueSpread * 0.1f);
        if (EffectTags.HasTag(KeyLibrary_QuestEffectTag.MedicalInfusion))
        {
            spreadCount *= 0.8f;
        }
        population = Math.Max(0, population - Mathf.FloorToInt(spreadCount));
        if (population < 500)
        {
            PlagueOutOfControl();
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
                CliquesManager.AdjustCliqueWillingness(KeyLibrary_QuestCliqueKey.Civilian, -0.1f * plagueSpread);
            }
            if (CliquesManager.HasClique(KeyLibrary_QuestCliqueKey.PanickedCivilian))
            {
                if (plagueSpread < 0.2f)
                {
                    CliquesManager.RemoveClique(KeyLibrary_QuestCliqueKey.PanickedCivilian);
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

    private void PlagueResolved()
    {
        hasSettled = true;
        SendWorkResolvedSignal();
        if (!Destroyed)
        {
            Destroy();
        }
    }

    private void PlagueOutOfControl()
    {
        hasSettled = true;

        if (!Destroyed)
        {
            Destroy();
        }
    }
}
