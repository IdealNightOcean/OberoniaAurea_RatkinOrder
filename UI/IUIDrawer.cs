using OberoniaAurea_Frame;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public interface IUIDrawer
{
    Vector2 DefaultSize { get; }
    Vector2 DrawSize { get; }
    TextStyle TextStyle { get; }
}
