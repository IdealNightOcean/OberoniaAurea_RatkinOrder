using OberoniaAurea_Frame.UI;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public interface IUIDrawer
{
    Vector2 DefaultSize { get; }
    int OutlineThickness { get; }
    Vector2 DrawSize { get; }
    TextStyle TextStyle { get; }
}
