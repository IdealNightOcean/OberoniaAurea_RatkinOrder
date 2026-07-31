using OberoniaAurea_Frame.UI;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDrawerBase : IUIDrawer
{
    protected Vector2? sizeOverride;
    public virtual Vector2 DefaultSize { get; } = new(800, 600);
    public Vector2 DrawSize => sizeOverride ?? DefaultSize;
    public TextStyle TextStyle { get; protected set; } = TextStyle.DefaultStyle;

    public void SetDrawSize(Vector2 size) => sizeOverride = size;
}
