using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士属性Def
/// </summary>
public class ResidentKnightStatDef : OAROStatDefBase
{
    private ResidentKnightStatWorker worker;
    public ResidentKnightStatWorker Worker => worker ??= new ResidentKnightStatWorker(this);

    /// <summary>
    /// 额外Stat修正器列表（<see cref="ResidentKnightStatPart"/>），可为 <see langword="null"/>
    /// </summary>
    public List<ResidentKnightStatPart> statParts;

    public override void PostLoad()
    {
        base.PostLoad();
        statParts?.SortByDescending(part => part.Priority);
    }
}
