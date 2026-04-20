using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士团交互Def
/// </summary>
public class OrderInteractionDef : InteractionDefBase
{
    public Type workerClass;
    private OrderInteractionWorker worker;
    public OrderInteractionWorker Worker => worker ??= (OrderInteractionWorker)Activator.CreateInstance(workerClass, args: this);

    /// <summary>
    /// 是否在骑士团主界面UI上显式
    /// </summary>
    public bool displayOnUI;

    /// <summary>是否在骑士团主界面UI上特殊显示</summary>
    /// <remarks>
    /// <para>- 只在 <see cref="displayOnUI"/> 为 <see langword="true"/> 时生效</para>
    /// <para>- 若为 <see langword="true"/> 则需在骑士团主界面手动实现显示</para>
    /// <para>- 若为 <see langword="false"/> 则自动在骑士团主界面相关部分集中显示</para>
    /// </remarks>
    public bool specialDisplayOnUI;

    /// <summary>
    /// 需求骑士团资金数
    /// </summary>
    public float needFund = -1f;

    /// <summary>事件触发的资金事件</summary>
    /// <remarks>- 只在 <see cref="needFund"/>><see langword="0f"/> 时生效</remarks>
    public OrderFundEventDef fundEventDef;

    /// <summary>
    /// 最低骑士团资金需求
    /// </summary>
    public float MinFundNeeded => needFund > 0f ? needFund : (fundEventDef is null ? 0f : fundEventDef.changeRange.min);

    public AcceptanceReport CanUseInteraction(RatkinOrder ratkinOrder, Map map, bool resultOnly)
    {
        if (!ratkinOrder.IsValid() || Worker is null)
        {
            return false;
        }
        return Worker.CanUseInteraction(ratkinOrder, map, resultOnly);
    }

    public void TryApplyInteraction(RatkinOrder ratkinOrder, Map map)
    {
        if (ratkinOrder.IsValid())
        {
            Worker?.TryApplyInteraction(ratkinOrder, map);
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (workerClass is null)
        {
            yield return $"'{nameof(workerClass)}' 为 null。";
        }

        if (needFund > 0f && fundEventDef is not null)
        {
            yield return $"不能同时设置 '{nameof(needFund)}' 和 '{nameof(fundEventDef)}'。";
        }
    }
}