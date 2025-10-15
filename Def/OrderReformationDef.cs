using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士团自新Def
/// </summary>
public class OrderReformationDef : Def
{
    private static readonly Type defaultWorkerClass = typeof(OrderReformationWorker);
    private static readonly OrderReformationWorker defaultWorker = new(null);
    public Type workerClass = defaultWorkerClass;
    private OrderReformationWorker worker;
    public OrderReformationWorker Worker => worker ??= (workerClass == defaultWorkerClass ? defaultWorker : (OrderReformationWorker)Activator.CreateInstance(workerClass, this));

    public ReformationType reformationType;
    public float reformProgressCost;

    public List<OrderReformationDef> prerequisites;

    public List<string> effectFlags; //效果标志列表
    public List<BranchStatModifier> branchStatModifies; //属性修正列表
}
