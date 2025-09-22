using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Worldobject_PlagueVillage : WorldObject_InteractWithFixedCaravan_Nameable, ISingleBranchRelated
{
    public override int TicksNeeded => 30000;

    private int branchPolicy; // 1:秩序管控，2:样本收集，3:调查协助
    private int workType; // 1:协助救治，2:协助隔离

    [Unsaved] private bool settled;

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
    public float PlagueControl
    {
        get => plagueControl;
        set => plagueControl = Mathf.Clamp(value, 0f, maxPlagueControl);
    }

    private Branch branch;
    public Branch Branch => branch;

    [Unsaved] QuestPart_EffectTags effectTags;
    private QuestPart_EffectTags EffectTags => effectTags ?? (QuestPart_EffectTags.TryGetEffectTags(quest, addPartIfMiss: false, out effectTags) ? effectTags : null);

    [Unsaved] QuestPart_CliquesManager cliquesManager;
    private QuestPart_CliquesManager CliquesManager => cliquesManager ?? (QuestPart_CliquesManager.TryGetCliquesManager(quest, addPartIfMiss: false, out cliquesManager) ? cliquesManager : null);
    private float TotalPotency => CliquesManager?.TotalPotency ?? 0f;

    public override void PostAdd()
    {
        originalPopulation = Rand.RangeInclusive(3000, 4000);
        population = originalPopulation;

        PlagueSpread = Rand.Range(0.3f, 0.45f);
        PlagueControl = 0;

        nextPeriodicCheckTick = Find.TickManager.TicksGame + 15000;
        nextPlagueSpreadTick = Find.TickManager.TicksGame + 60000;

        if (EffectTags is not null)
        {
            if (effectTags.HasTag(KeyLibrary_QuestEffectTag.Panic))
            {

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
                        PlagueControl += controlAdd;
                        break;
                    }
                case 2:
                    {
                        int totalSocialLevel = PawnUtility.GetTotalSkillLevelOf(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Social);
                        float spreadReduce = totalSocialLevel * 0.00025f;
                        PlagueSpread -= spreadReduce;
                        break;
                    }
                default: break;
            }
        }
    }

    protected override void InterruptWork() { }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (settled)
        {
            return;
        }

        if (Find.TickManager.TicksGame >= nextPeriodicCheckTick)
        {
            nextPeriodicCheckTick = Find.TickManager.TicksGame + 15000;
            PeriodicCheck();
            if (settled)
            {
                return;
            }

            if (Find.TickManager.TicksGame >= nextPlagueSpreadTick)
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
                    PlagueControl += controlAdd;
                    Messages.Message("OARO_PlagueVillage_OrderControlResult".Translate(plagueSpreadReduce.ToStringPercent("F2"), controlAdd.ToString("F2")), MessageTypeDefOf.PositiveEvent, historical: true);

                    if (PlagueControl >= maxPlagueControl)
                    {
                        PlagueResolved();
                        return;
                    }
                }


                break;
            case 2:
                if (Rand.Chance(TotalPotency * 0.6f))
                {

                }
                break;
            case 3:
                {
                    float controlAdd = 2 + TotalPotency * 12f;
                    if (EffectTags?.HasTag(KeyLibrary_QuestEffectTag.StrangePlague) ?? false)
                    {
                        controlAdd *= 0.5f;
                    }
                    PlagueControl += controlAdd;
                    Messages.Message("OARO_PlagueVillage_SurveyAssistanceResult".Translate(controlAdd.ToString("F2")), MessageTypeDefOf.PositiveEvent, historical: true);

                    if (PlagueControl >= maxPlagueControl)
                    {
                        PlagueResolved();
                        return;
                    }
                    break;
                }
            default:
                break;
        }

        if (!settled && Find.TickManager.TicksGame >= nextPeriodicCheckTick)
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
                    PlagueControl -= plagueImpact;
                    Messages.Message("OARO_PlagueVillage_PanickedPlagueImpact".Translate(plagueImpact), MessageTypeDefOf.NegativeEvent, historical: true);
                }
            }
        }

        PlagueSpread += (0.02f + PlagueSpread * 0.05f);
    }

    private void PlagueResolved()
    {
        settled = true;
        SendWorkResolvedSignal();
        if (isWorking)
        {
            EndWork(interrupt: true, convertToCaravan: true);
        }
        if (!Destroyed)
        {
            Destroy();
        }
    }

    private void PlagueOutOfControl()
    {
        settled = true;
        if (isWorking)
        {
            EndWork(interrupt: true, convertToCaravan: true);
        }

        if (!Destroyed)
        {
            Destroy();
        }
    }
}
