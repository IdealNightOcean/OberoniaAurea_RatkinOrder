using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 狼灾检查点（特化类）
/// </summary>
internal sealed class WorldObject_WolfDisasterPoint : WorldObject_InteractWithFixedCaravan_Nameable
{
    private enum WorkType : byte
    {
        EstablishObservation,
        ObtainIntelligence
    }

    private bool observationEstablished;
    private bool intelligenceObtained;
    private int nextCanObtainSuppliesTick = -1;

    private WorkType curWork;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref curWork, nameof(curWork));

        Scribe_Values.Look(ref observationEstablished, nameof(observationEstablished), defaultValue: false);
        Scribe_Values.Look(ref intelligenceObtained, nameof(intelligenceObtained), defaultValue: false);
        Scribe_Values.Look(ref nextCanObtainSuppliesTick, nameof(nextCanObtainSuppliesTick), -1);
    }

    public override int TicksNeeded => curWork switch
    {
        WorkType.EstablishObservation => 30000,
        WorkType.ObtainIntelligence => 15000,
        _ => 30000
    };

    public override bool StartWork(Caravan caravan)
    {
        WorkDialog(caravan);
        return true;
    }

    private void WorkDialog(Caravan caravan)
    {
        DiaNode rootNode = new("OARO_WolfDisasterPoint_Root".Translate());
        DiaOption obtainSuppliesOpt = new($"OARO_WolfDisasterPoint_ObtainSupplies".Translate())
        {
            action = () => ObtainSupplies(caravan),
            resolveTree = true,
        };
        int coolingTicksLeft = nextCanObtainSuppliesTick - Find.TickManager.TicksGame;
        if (coolingTicksLeft > 0)
        {
            obtainSuppliesOpt.Disable("WaitTime".Translate(coolingTicksLeft.ToStringTicksToPeriod()));
        }
        rootNode.options.Add(obtainSuppliesOpt);

        if (!observationEstablished)
        {
            DiaOption establishObservationOpt = new($"OARO_WolfDisasterPoint_{WorkType.EstablishObservation}".Translate())
            {
                action = delegate
                {
                    curWork = WorkType.EstablishObservation;
                    base.StartWork(caravan);
                },
                resolveTree = true,
            };
            rootNode.options.Add(establishObservationOpt);
        }


        if (!intelligenceObtained)
        {
            DiaOption establishObservationOpt = new($"OARO_WolfDisasterPoint_{WorkType.ObtainIntelligence}".Translate())
            {
                action = delegate
                {
                    curWork = WorkType.ObtainIntelligence;
                    base.StartWork(caravan);
                },
                resolveTree = true,
            };
            rootNode.options.Add(establishObservationOpt);
        }

        rootNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);

        Dialog_NodeTreeWithFactionInfo nodeTree = new(rootNode, Faction);
        Find.WindowStack.Add(nodeTree);
    }

    private void ObtainSupplies(Caravan caravan)
    {
        nextCanObtainSuppliesTick = Find.TickManager.TicksGame + 30000;
        Thing meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
        int count = caravan.PawnsListForReading.Count * 4;
        meal.stackCount = count;
        CaravanInventoryUtility.GiveThing(caravan, meal);
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                       text: "OARO_WolfDisasterPoint_SuppliesObtained".Translate(count.Named(KeyLibrary_FormatArgName.Count)),
                       faction: Faction));
    }

    protected override void FinishWork()
    {
        switch (curWork)
        {
            case WorkType.EstablishObservation:
                {
                    observationEstablished = true;
                    QuestUtility.SendQuestTargetSignals(questTags, "ObservationEstablished");
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                        text: "OARO_WolfDisasterPoint_EstablishObservation_Finished".Translate(),
                        faction: Faction));
                    return;
                }
            case WorkType.ObtainIntelligence:
                {
                    intelligenceObtained = true;
                    QuestUtility.SendQuestTargetSignals(questTags, "IntelligenceObtained");
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
                        text: "OARO_WolfDisasterPoint_ObtainIntelligence_Finished".Translate(),
                        faction: Faction));
                    return;
                }

            default: return;
        }
    }

    protected override void InterruptWork() { }
}