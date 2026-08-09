using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataBase : IUIData
{
    public UIDataState DataState { get; protected set; } = UIDataState.Dirty;

    /// <summary>
    /// 数据有效可用
    /// </summary>
    public bool IsDataValid => DataState == UIDataState.Ready;

    /// <summary>
    /// 允许绘制：空占位 / 正常有效数据
    /// </summary>
    public virtual bool CanDraw => DataState is UIDataState.Empty or UIDataState.Ready;

    /// <summary>
    /// 标记数据过期，下次绘制前应当刷新
    /// </summary>
    public void MarkDirty() => DataState = UIDataState.Dirty;

    public void Refresh()
    {
        DataState = RefreshInner();

        if (DataState == UIDataState.Dirty)
        {
            Log.Error($"[OARO] {GetType().Name}.RefreshInner 返回 {nameof(UIDataState.Dirty)} 。{nameof(UIDataState.Dirty)} 仅用作待刷新标记，不能作为刷新完成结果。");
            DataState = UIDataState.Invalid;
        }
    }

    /// <summary>
    /// 执行实际数据校验逻辑
    /// <para>不可返回 <see cref="UIDataState.Dirty"/>；该状态仅用于外部标记数据过期，不能作为刷新完成结果。</para>
    /// </summary>
    /// <returns>刷新完成后的业务状态</returns>
    protected abstract UIDataState RefreshInner();
}