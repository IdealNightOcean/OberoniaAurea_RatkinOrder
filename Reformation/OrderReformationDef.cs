using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderReformationDef : Def
{
    private static readonly Type DefaultWorkerClass = typeof(OrderReformationWorker);
    private static readonly OrderReformationWorker DefaultWorker = new(null);
    public Type workerClass = DefaultWorkerClass;
    private OrderReformationWorker worker;
    public OrderReformationWorker Worker => worker ??= (workerClass == DefaultWorkerClass ? DefaultWorker : (OrderReformationWorker)Activator.CreateInstance(workerClass, this));

    public float reformProgressCost;

    public List<OrderReformationDef> prerequisites;

    public List<string> effectFlags; //效果标志列表
    public List<BranchStatModifier> branchStatModifies; //属性修正列表
}
