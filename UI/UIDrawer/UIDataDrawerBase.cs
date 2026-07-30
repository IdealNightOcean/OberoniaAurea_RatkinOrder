using OberoniaAurea_Frame;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataDrawerBase<T> : IUIDrawer where T : UIDataBase
{
    protected Vector2? sizeOverride;
    public virtual Vector2 DefaultSize { get; } = new(800, 600);
    public Vector2 DrawSize => sizeOverride ?? DefaultSize;
    public TextStyle TextStyle { get; protected set; } = TextStyle.DefaultStyle;

    protected bool DrawDataValid { get; private set; }

    public void SetDrawSize(Vector2 size) => sizeOverride = size;

    public void Draw(Vector2 position, T drawData)
    {
        if (drawData is null)
        {
            Log.Error("[OARO] 绘制数据源 drawData 不能为空，请传入有效的 UIData 实例");
            return;
        }

        drawData.Refresh();
        DrawDataValid = drawData.IsValid;
        DrawInner(position, drawData);
        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    public abstract void DrawInner(Vector2 position, T drawData);

}