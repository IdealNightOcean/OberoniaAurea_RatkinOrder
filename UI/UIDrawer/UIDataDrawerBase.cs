using OberoniaAurea_Frame;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataDrawerBase<T> : IUIDrawer where T : UIDataBase
{
    public virtual Vector2 InitSize { get; } = new(800, 600);
    public TextStyle_GameFont GameFontText { get; protected set; } = TextStyle_GameFont.DefaultStyle;
    public TextStyle_FontSize FontSizeText { get; protected set; } = TextStyle_FontSize.DefaultStyle;

    public void Draw(Vector2 position, T drawData)
    {
        drawData.Refresh();
        DrawInner(position, drawData);
    }

    public abstract void DrawInner(Vector2 position, T drawData);

}