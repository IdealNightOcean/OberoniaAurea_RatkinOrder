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

    /// <summary>
    /// 自新功能类
    /// </summary>
    public Type workerClass = defaultWorkerClass;
    private OrderReformationWorker worker;
    public OrderReformationWorker Worker => worker ??= (workerClass == defaultWorkerClass ? defaultWorker : (OrderReformationWorker)Activator.CreateInstance(workerClass, this));

    /// <summary>
    /// 自新类型
    /// </summary>
    public ReformationType reformationType;

    /// <summary>
    /// 自新值花费
    /// </summary>
    public float reformProgressCost;

    /// <summary>
    /// 前置自新
    /// </summary>
    public List<OrderReformationDef> prerequisites;

    /// <summary>
    /// 效果标志列表
    /// </summary>
    public List<string> effectFlags;

    /// <summary>
    /// 属性修正列表（Offset）
    /// </summary>
    public List<BranchStatModifier> branchStatOffsets;

    /// <summary>
    /// 属性修正列表（Factor）
    /// </summary>
    public List<BranchStatModifier> branchStatFactors;
}
