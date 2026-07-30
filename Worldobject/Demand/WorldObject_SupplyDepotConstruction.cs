using OberoniaAurea.RatkinOrder.UI;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public sealed class WorldObject_SupplyDepotConstruction : WorldObject_InteractWithFixedCaravan_Nameable, ISingleBranchRelated
{
    private Branch branch;
    public Branch Branch => branch;
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;

    private float constricProgress = 0f;
    private bool autoCotrInofrmed;
    private int ticksToNextAutoCtor;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, nameof(branch));
        Scribe_Values.Look(ref constricProgress, nameof(constricProgress), 0f);
        Scribe_Values.Look(ref autoCotrInofrmed, nameof(autoCotrInofrmed), defaultValue: false);
        Scribe_Values.Look(ref ticksToNextAutoCtor, nameof(ticksToNextAutoCtor), 2500);
    }

    public void SetOrderBranch(Branch branch) => this.branch = branch;

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine("OARO_SupplyDepot_ConstricProgress".Translate(constricProgress.ToString("0.##"), 800));
        if (constricProgress >= 400f)
        {
            sb.AppendInNewLine("OARO_SupplyDepot_AutoConstructing".Translate());
        }

        return sb.ToString();
    }

    public override bool StartWork(Caravan caravan)
    {
        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_UIUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_SupplyDepot_ArrivalInfo".Translate(TicksNeeded.ToStringTicksToPeriod()),
            ratkinOrder: branch.RatkinOrder,
            acceptAction: () => base.StartWork(caravan));

        Find.WindowStack.Add(nodeTree);
        return true;
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (constricProgress > 400f)
        {
            if ((ticksToNextAutoCtor -= delta) <= 0)
            {
                ticksToNextAutoCtor = 2500;
                constricProgress = Mathf.Clamp(constricProgress + 10f, 0f, 800f);
                if (constricProgress >= 800f && !isWorking)
                {
                    this.SendWorkResolvedSignal();
                    ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
                        label: "OARO_SupplyDepot_FinallyAutoFinishedLabel".Translate(),
                        text: "OARO_SupplyDepot_FinallyAutoFinishedText".Translate(),
                        def: OARO_LetterDefOf.OARO_Order_PositiveLetter,
                        lookTargets: this,
                        relatedFaction: RatkinOrder?.Faction,
                        quest: quest);
                    letter.RelatedOrder = RatkinOrder;
                    Find.LetterStack.ReceiveLetter(letter);
                    this.SafeDestroy();
                }
            }
        }
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is null)
        {
            return;
        }

        int totalSkillLevel = associatedFixedCaravan.PawnsListForReading.Sum(p => p.skills?.GetSkill(SkillDefOf.Construction).GetLevel() ?? 0);
        float gainProgress = totalSkillLevel * 4f + associatedFixedCaravan.PawnsCount * 20f;
        if (constricProgress >= 400f)
        {
            gainProgress *= 1.5f;
        }
        constricProgress = Mathf.Clamp(constricProgress + gainProgress, 0f, 800f);

        if (constricProgress >= 800f)
        {
            this.SendWorkResolvedSignal();
            Find.WindowStack.Add(OARO_UIUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
                text: "OARO_SupplyDepot_FinallyFinished".Translate(),
                ratkinOrder: RatkinOrder));

            PlanetTile tile = Tile;
            this.SafeDestroy();
        }
        else
        {
            if (!autoCotrInofrmed && constricProgress >= 400f)
            {
                autoCotrInofrmed = true;
                ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
                    label: "OARO_SupplyDepot_AutoCtorAvailableLabel".Translate(),
                    text: "OARO_SupplyDepot_AutoCtorAvailableText".Translate(),
                    def: OARO_LetterDefOf.OARO_Order_PositiveLetter,
                    lookTargets: this,
                    relatedFaction: RatkinOrder?.Faction,
                    quest: quest);
                letter.RelatedOrder = RatkinOrder;
                Find.LetterStack.ReceiveLetter(letter);
            }
            else
            {
                Find.WindowStack.Add(OARO_UIUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
                    text: "OARO_SupplyDepot_Finished".Translate(gainProgress.ToString("0.##")),
                    ratkinOrder: RatkinOrder));
            }
        }
    }
    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OARO_UIUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo("OARO_SupplyDepot_InterruptWork".Translate(), RatkinOrder));
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.SafeDestroy();
        }
    }
    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            this.SafeDestroy();
        }
    }
}
