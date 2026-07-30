using OberoniaAurea.RatkinOrder.DataLibrary;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchDemand;

namespace OberoniaAurea.RatkinOrder;

internal sealed class QuestNode_GenerateFaltSuperHeavyHowitzer : QuestNode
{
    public SlateRef<DemandType?> demandType;

    [NoTranslate]
    public SlateRef<string> storeHowitzerAs;
    [NoTranslate]
    public SlateRef<string> storeRewardSilverCountAs;
    [NoTranslate]
    public SlateRef<string> storePerfectRewardSilverCountAs;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        DemandType demandType = this.demandType.GetValue(slate) ?? slate.Get<DemandType>(OARO_KeyLibrary_SlateStoreAs.demandType);
        int rewardSilverCount;
        int perfectRewardSilverCount;
        List<Thing> howitzers = [];

        if (demandType == DemandType.Supplementary)
        {
            Thing howitzer = ThingMaker.MakeThing(OARO_ThingDefOf.OARO_Turret_OrderSuperHeavyHowitzer);
            CompSuperHeavyHowitzer howitzerComp = howitzer.TryGetComp<CompSuperHeavyHowitzer>();
            int normalFalt = Rand.RangeInclusive(4, 6);
            howitzerComp.InitFault(normalFalt, latentFault: 0);
            rewardSilverCount = perfectRewardSilverCount = normalFalt * 95;

            howitzer.SetFaction(Faction.OfPlayer);
            Thing howitzerMini = MinifyUtility.TryMakeMinified(howitzer);
            howitzers.Add(howitzerMini);
        }
        else
        {
            int totalNormalFalt = 0;
            int totalLatentFault = 0;

            for (int i = 0; i < 2; i++)
            {
                Thing howitzer = ThingMaker.MakeThing(OARO_ThingDefOf.OARO_Turret_OrderSuperHeavyHowitzer);
                CompSuperHeavyHowitzer howitzerComp = howitzer.TryGetComp<CompSuperHeavyHowitzer>();
                int normalFalt = Rand.RangeInclusive(4, 6);
                int latentFalt = Rand.RangeInclusive(2, 3);
                howitzerComp.InitFault(normalFalt, latentFalt);
                totalNormalFalt += normalFalt;
                totalLatentFault += latentFalt;

                howitzer.SetFaction(Faction.OfPlayer);
                Thing howitzerMini = MinifyUtility.TryMakeMinified(howitzer);
                howitzers.Add(howitzerMini);
            }

            if (demandType == DemandType.Urgency)
            {
                rewardSilverCount = 140 * totalLatentFault;
                perfectRewardSilverCount = 160 * (totalLatentFault + 3 * totalLatentFault);
            }
            else
            {
                rewardSilverCount = 95 * totalLatentFault;
                perfectRewardSilverCount = 100 * (totalLatentFault + 3 * totalLatentFault);
            }

        }

        slate.Set(storeRewardSilverCountAs.GetValue(slate), rewardSilverCount);
        slate.Set(storePerfectRewardSilverCountAs.GetValue(slate), perfectRewardSilverCount);
        slate.Set(storeHowitzerAs.GetValue(slate), howitzers);
    }
}
