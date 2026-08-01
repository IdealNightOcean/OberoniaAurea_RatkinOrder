using OberoniaAurea_Frame.UI;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDrawerBase : IUIDrawer
{
    protected Vector2? sizeOverride;
    public Vector2 DefaultSize { get; protected set; } = new(800, 600);
    public Vector2 DrawSize => sizeOverride ?? DefaultSize;
    public int OutlineThickness { get; protected set; } = 1;

    public TextStyle TextStyle { get; protected set; } = TextStyle.DefaultStyle;

    public void SetDrawSize(Vector2 size) => sizeOverride = size;
}
