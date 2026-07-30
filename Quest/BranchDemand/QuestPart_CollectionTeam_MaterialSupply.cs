using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 需求-物资补给任务监控 QuestPart | TalkAction（内部特化类）
/// </summary>
internal sealed class QuestPart_CollectionTeam_MaterialSupply : QuestPart_CollectionTeam
{
    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        if (Branch.IsValid() && quest.State == QuestState.EndedSuccess)
        {
            Branch.Supply += 0.5f;
        }
    }
}