using OberoniaAurea_Frame.UI;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public interface IUIDrawer
{
    int OutlineThickness { get; }
    Vector2 DrawSize { get; }
    TextStyle TextStyle { get; }
}
