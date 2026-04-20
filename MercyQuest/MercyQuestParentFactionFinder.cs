using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 善行任务父派系查找器基类
/// </summary>
public abstract class MercyQuestParentFactionFinder
{
    public abstract Faction FindParentFaction(MercyQuestDef mercyDef, FactionValidationParams? factionParams = null, FactionDef fixedParentFactionDef = null);
}