using OberoniaAurea_Frame;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public interface IUIDrawer
{
    Vector2 InitSize { get; }
    TextStyle_GameFont GameFontText { get; }
    TextStyle_FontSize FontSizeText { get; }
}
