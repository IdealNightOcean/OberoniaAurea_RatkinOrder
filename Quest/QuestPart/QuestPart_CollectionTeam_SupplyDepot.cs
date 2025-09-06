using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 需求-物资补给任务监控 QuestPart | TalkAction（内部特化类）
/// </summary>
internal sealed class QuestPart_CollectionTeam_SupplyDepot : QuestPart_CollectionTeam
{
    protected override TaggedString GetTalkNodeText(Pawn talker, Pawn talkWith)
    {
        return "OARO_Demand_SupplyDepotCotrTeamInfo".Translate(talkWith) + "\n\n" + RequestThingsSummary();
    }
}
