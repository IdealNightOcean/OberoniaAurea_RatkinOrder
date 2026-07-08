using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部属性Def
/// </summary>
public class BranchStatDef : OAROStatDefBase
{
    private static readonly Type defaultWorker = typeof(BranchStatWorker);

    private BranchStatWorker worker;

    public BranchStatWorker Worker => worker ??= (BranchStatWorker)Activator.CreateInstance(workerClass, this);

    public List<BranchStatPart> statParts;


    public BranchStatDef() : base()
    {
        workerClass = defaultWorker;
    }

    public override void PostLoad()
    {
        base.PostLoad();
        statParts?.SortByDescending(part => part.Priority);
    }
}
