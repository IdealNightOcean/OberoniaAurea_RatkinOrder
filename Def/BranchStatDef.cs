using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部属性Def
/// </summary>
public class BranchStatDef : OAROStatDefBase
{
    private BranchStatWorker worker;
    public BranchStatWorker Worker => worker ??= new BranchStatWorker(this);

    /// <summary>
    /// 额外Stat修正器列表（<see cref="BranchStatPart"/>），可为 <see langword="null"/>
    /// </summary>
    public List<BranchStatPart> statParts;

    public override void PostLoad()
    {
        base.PostLoad();
        statParts?.SortByDescending(part => part.Priority);
    }
}
