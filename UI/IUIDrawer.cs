using OberoniaAurea_Frame;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public interface IUIDrawer
{
    Vector2 InitSize { get; }
    TextStyle TextStyle { get; }
}
