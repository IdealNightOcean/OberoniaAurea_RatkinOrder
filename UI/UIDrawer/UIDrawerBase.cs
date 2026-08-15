using OberoniaAurea_Frame.UI;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDrawerBase : IUIDrawer
{
    public Vector2 DrawSize { get; protected set; } = new(800, 600);
    public Vector2 ValidDrawSize => new(DrawSize.x - 2 * OutlineThickness, DrawSize.y - 2 * OutlineThickness);
    public int OutlineThickness { get; protected set; } = 1;

    public TextStyle TextStyle { get; protected set; } = TextStyle.DefaultStyle;

    /// <summary>
    /// 设置绘制尺寸
    /// </summary>
    public bool SetDrawSize(Vector2 size)
    {
        if (size.x > 1e-6f && size.y > 1e-6f)
        {
            DrawSize = size;
            return true;
        }

        return false;
    }

    public bool SetDrawSizeAspectFit(Vector2 containerSize)
    {
        if (containerSize.x <= 1e-6f || containerSize.y <= 1e-6f)
            return false;

        float scaledHeight = DrawSize.y * containerSize.x / DrawSize.x;
        if (scaledHeight < containerSize.y)
        {
            return SetDrawSizeByWidth(containerSize.x);
        }
        else
        {
            return SetDrawSizeByHeight(containerSize.y);
        }
    }

    /// <summary>
    /// 固定宽度，按原始宽高比自动计算高度
    /// </summary>
    public bool SetDrawSizeByWidth(float width)
    {
        if (DrawSize.x < 1e-6f || width < 1e-6f)
            return false;

        return SetDrawSize(new Vector2(width, DrawSize.y * width / DrawSize.x));
    }

    /// <summary>
    /// 固定高度，按原始宽高比自动计算宽度
    /// </summary>
    public bool SetDrawSizeByHeight(float height)
    {
        if (DrawSize.x < 1e-6f || height < 1e-6f)
            return false;

        return SetDrawSize(new Vector2(DrawSize.x * height / DrawSize.y, height));
    }

    /// <summary>
    /// 按缩放系数整体缩放DrawSize
    /// </summary>
    public bool ScaleDrawSize(float scaleFactor)
    {
        if (scaleFactor > 1e-6f)
        {
            Vector2 newDrawSize = DrawSize * scaleFactor;
            return SetDrawSize(newDrawSize);
        }

        return false;
    }
}
