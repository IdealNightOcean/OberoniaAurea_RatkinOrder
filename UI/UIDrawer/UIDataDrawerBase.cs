using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataDrawerBase<T> : UIDrawerBase where T : IUIData
{
    protected T DrawData { get; private set; }


    public virtual void SetDrawData(T drawData) => this.DrawData = drawData;

    public void Draw(Vector2 position)
    {
        if (this.DrawData is null)
        {
            Log.Error("[OARO] 绘制数据源 DrawData 不能为空，请先设置有效的 DrawData 实例");
            return;
        }

        if (this.DrawData.DataState == UIDataState.Dirty)
            this.DrawData.Refresh();

        if (this.DrawData.CanDraw)
            DrawInner(position);

        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    public abstract void DrawInner(Vector2 position);

}