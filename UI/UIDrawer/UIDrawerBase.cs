using OberoniaAurea_Frame.UI;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDrawerBase : IUIDrawer
{
    public Vector2 DrawSize { get; protected set; } = new(800, 600);
    public int OutlineThickness { get; protected set; } = 1;

    public TextStyle TextStyle { get; protected set; } = TextStyle.DefaultStyle;

    public void SetDrawSize(Vector2 size) => DrawSize = size;
}
