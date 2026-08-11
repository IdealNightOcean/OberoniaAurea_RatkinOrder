using NightOcean.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_SelectableList<T, U> : UIDrawerBase where T : IUIData where U : UIDataDrawerBase<T>
{
    private const float ScrollBarThickness = 16f;
    public U Drawer { get; protected set; }
    public IList<T> DrawDatas { get; protected set; }
    public int SelectedIndex { get; protected set; } = -1;
    public bool HasSelectedItem => SelectedIndex >= 0 && SelectedIndex < DrawDatas.Count;
    public T SelectedItem
    {
        get
        {
            if (DrawDatas is null || DrawDatas.Count == 0)
                return default;
            if (SelectedIndex < 0 || SelectedIndex >= DrawDatas.Count)
                return default;
            return DrawDatas[SelectedIndex];
        }
    }

    public int RowLimit { get; protected set; } = -1;
    public int ColumnLimit { get; protected set; } = -1;

    public bool HorizontalWarp { get; protected set; } = false;

    protected Vector2 scrollPosition = Vector2.zero;
    protected bool onSelecting = false;

    /// <summary>
    /// int：当前的选择索引，bool：索引是否发生了变化
    /// </summary>
    public EventDispatcher<Action<int, bool>> OnSelectedItem { get; } = new();

    public UIDataDrawer_SelectableList(U drawer, IList<T> drawDatas, int rowLimit = -1, int columnLimit = -1, bool horizontalWarp = false)
    {
        Drawer = drawer;
        DrawDatas = drawDatas;
        RowLimit = rowLimit;
        ColumnLimit = columnLimit;
        HorizontalWarp = horizontalWarp;
    }

    public virtual void SetDrawer(U drawer)
    {
        Drawer = drawer;
        ResetSelection();
    }

    public virtual void SetDrawDatas(IList<T> drawDatas)
    {
        DrawDatas = drawDatas;
        ResetSelection();
    }

    public virtual void ResetSelection()
    {
        SelectItem(-1);
    }

    public void SetScorllLimit(int rowLimit = -1, int columnLimit = -1, bool horizontalWarp = false)
    {
        RowLimit = rowLimit;
        ColumnLimit = columnLimit;
        HorizontalWarp = horizontalWarp;
    }

    /// <returns>选择索引是否发生变化</returns>
    public bool SelectItem(int index, bool applySelectionEvent = true)
    {
        if (onSelecting)
            return false;

        onSelecting = true;
        bool result = DoSelectItem(index, applySelectionEvent);
        onSelecting = false;
        return result;
    }


    /// <returns>选择索引是否发生变化</returns>
    protected virtual bool DoSelectItem(int index, bool applySelectionEvent = true)
    {
        if (index >= 0 && DrawDatas is not null && index < DrawDatas.Count)
        {
            SelectedIndex = SelectedIndex == index ? -1 : index;
            if (applySelectionEvent)
                ApplyOnSelectedItem(selectedIndexChanged: true);
            return true;
        }
        else if (SelectedIndex != -1)
        {
            SelectedIndex = -1;
            if (applySelectionEvent)
                ApplyOnSelectedItem(selectedIndexChanged: false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 触发 <see cref="OnSelectedItem"/> 事件
    /// </summary>
    public void ApplyOnSelectedItem(bool selectedIndexChanged)
    {
        OnSelectedItem.Raise(handler => handler(SelectedIndex, selectedIndexChanged));
    }

    public void UseRecommendOutRectSize()
    {
        Vector2 recommendSize = GetRecommendOutRectSize();
        if (recommendSize != Vector2.zero)
            SetDrawSize(recommendSize);
    }

    public Vector2 GetRecommendOutRectSize()
    {
        Vector2 entrySize = Drawer.DrawSize;
        float outlineThickness = Drawer.OutlineThickness;
        float stepX = entrySize.x - outlineThickness;
        float stepY = entrySize.y - outlineThickness;

        if (stepX <= 0f || stepY <= 0f)
            return Vector2.zero;

        Vector2 outRectSize = DrawSize;

        if (ColumnLimit > 0)
            outRectSize.x = ColumnLimit * stepX + outlineThickness;
        if (RowLimit > 0)
            outRectSize.y = RowLimit * stepY + outlineThickness;

        Rect viewRect = GetViewRect();
        if (HorizontalWarp)
        {
            if (viewRect.height > outRectSize.y)
            {
                outRectSize.y += ScrollBarThickness;
            }
        }
        else
        {
            if (viewRect.width > outRectSize.x)
            {
                outRectSize.x += ScrollBarThickness;
            }
        }
        return outRectSize;
    }

    public Rect GetViewRect()
    {
        if (DrawDatas is null || DrawDatas.Count == 0)
            return Rect.zero;

        Vector2 entrySize = Drawer.DrawSize;
        float outlineThickness = Drawer.OutlineThickness;
        float stepX = entrySize.x - outlineThickness;
        float stepY = entrySize.y - outlineThickness;

        if (stepX <= 0f || stepY <= 0f)
            return Rect.zero;

        int totalCount = DrawDatas.Count;
        int totalCols, totalRows;
        if (HorizontalWarp)
        {
            totalCols = ColumnLimit > 0 ? Math.Min(ColumnLimit, totalCount) : totalCount;
            totalRows = Mathf.CeilToInt((float)totalCount / totalCols);
        }
        else
        {
            totalRows = RowLimit > 0 ? Math.Min(RowLimit, totalCount) : totalCount;
            totalCols = Mathf.CeilToInt((float)totalCount / totalRows);
        }

        float totalWidth = Mathf.Max(1e-6f, totalCols * stepX + outlineThickness);
        float totalHeight = Mathf.Max(1e-6f, totalRows * stepY + outlineThickness);

        return new Rect(0f, 0f, totalWidth, totalHeight);
    }

    public void Draw(Vector2 position)
    {
        if (Drawer is null || DrawDatas is null || DrawDatas.Count == 0)
            return;

        Rect outRect = new(position, DrawSize);
        Rect viewRect = GetViewRect();

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        Vector2 entryPos = Vector2.zero;
        Vector2 entrySize = Drawer.DrawSize;
        float offsetX = entrySize.x - Drawer.OutlineThickness;
        float offsetY = entrySize.y - Drawer.OutlineThickness;
        int curRow = 1;
        int curColumn = 1;

        Rect visibleRect = new(scrollPosition, outRect.size);
        for (int i = 0; i < DrawDatas.Count; i++)
        {
            Rect entryRect = new(entryPos, entrySize);
            if (!entryRect.Overlaps(visibleRect))
            {
                UpdateNextPosition();
                continue;
            }

            DrawEntry(entryRect, i);

            UpdateNextPosition();
        }
        Widgets.EndScrollView();


        void UpdateNextPosition()
        {
            if (HorizontalWarp)
            {
                if (ColumnLimit > 0 && curColumn >= ColumnLimit)
                {
                    curColumn = 1;
                    curRow++;
                    entryPos.x = 0f;
                    entryPos.y += offsetY;
                }
                else
                {
                    curColumn++;
                    entryPos.x += offsetX;
                }
            }
            else
            {
                if (RowLimit > 0 && curRow >= RowLimit)
                {
                    curRow = 1;
                    curColumn++;
                    entryPos.x += offsetX;
                    entryPos.y = 0f;
                }
                else
                {
                    curRow++;
                    entryPos.y += offsetY;
                }
            }
        }
    }

    protected virtual void DrawEntry(Rect inRect, int dataIndex)
    {
        Drawer.SetDrawData(DrawDatas[dataIndex]);
        Drawer.Draw(inRect.TopLeftCorner());

        if (Widgets.ButtonInvisible(inRect))
            SelectItem(dataIndex);

        if (dataIndex == SelectedIndex)
        {
            Widgets.DrawBox(inRect);
            Widgets.DrawHighlightSelected(inRect);
        }
        else if (Mouse.IsOver(inRect))
            Widgets.DrawHighlight(inRect);
    }
}