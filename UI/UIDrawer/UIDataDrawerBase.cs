using OberoniaAurea_Frame;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataDrawerBase<T> : IUIDrawer where T : UIDataBase
{
    public virtual Vector2 DefaultSize { get; } = new(800, 600);
    public TextStyle TextStyle { get; protected set; } = TextStyle.DefaultStyle;

    public void Draw(Vector2 position, T drawData)
    {
        drawData.Refresh();
        DrawInner(position, drawData);
        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    public abstract void DrawInner(Vector2 position, T drawData);

}