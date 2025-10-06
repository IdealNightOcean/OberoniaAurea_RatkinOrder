using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 需求-物资补给任务监控 QuestPart | TalkAction（内部特化类）
/// </summary>
internal sealed class QuestPart_CollectionTeam_MaterialSupply : QuestPart_CollectionTeam
{
    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        if (Branch is not null && quest.State == QuestState.EndedSuccess)
        {
            Branch.SquadStat.Supply += 0.5f;
        }
    }

    protected override TaggedString GetTalkNodeText(Pawn talker, Pawn talkWith)
    {
        return "OARO_Demand_MaterialSupplyInfo".Translate(talkWith) + "\n\n" + RequestThingsSummary();
    }
}