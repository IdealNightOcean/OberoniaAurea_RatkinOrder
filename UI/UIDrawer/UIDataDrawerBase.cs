using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataDrawerBase<T> : UIDrawerBase where T : IUIData
{
    protected T DrawData { get; private set; }
    protected bool DrawDataValid { get; private set; }

    public void SetDrawData(T drawData) => this.DrawData = drawData;

    public void Draw(Vector2 position)
    {
        if (this.DrawData is null)
        {
            Log.Error("[OARO] 绘制数据源 DrawData 不能为空，请先设置有效的 DrawData 实例");
            return;
        }

        this.DrawData.Refresh();
        DrawDataValid = this.DrawData.IsValid;
        DrawInner(position);
        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    public abstract void DrawInner(Vector2 position);

}