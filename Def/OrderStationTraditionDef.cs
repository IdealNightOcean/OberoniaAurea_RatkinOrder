using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士驻地传统<see cref="Def"/> - 定义了骑士驻地可能激活的传统效果，包括激活条件、相关骑士精神等信息
/// </summary>
public class OrderStationTraditionDef : Def
{
    public Type workerClass = typeof(OrderStationTraditionWorker);

    private OrderStationTraditionWorker worker;
    public OrderStationTraditionWorker Worker => worker ??= OrderStationTraditionWorker.CreateWorker(this);

    private KnightChivalryDef chivalryOverride;
    /// <summary>
    /// 对应骑士精神大类
    /// </summary>
    public KnightChivalryDef Chivalry => chivalryOverride ?? relatedVirtue.chivalry;

    /// <summary>
    /// 对应骑士美德（可选）
    /// </summary>
    public KnightVirtueDef relatedVirtue;

    /// <summary>
    /// 激活所需的4级个性骑士数量
    /// </summary>
    public int requiredKnightCount;

    /// <summary>
    /// 传统激活后给予殖民者的<see cref="HediffDef"/>
    /// </summary>
    public HediffDef colonistHediff;

    /// <summary>
    /// 传统激活后给予骑士的<see cref="HediffDef"/>
    /// </summary>
    public HediffDef knightHediff;

    /// <summary>
    /// 对应大类课业修行花费减免比例
    /// </summary>
    public float academicCostReduction;
}
