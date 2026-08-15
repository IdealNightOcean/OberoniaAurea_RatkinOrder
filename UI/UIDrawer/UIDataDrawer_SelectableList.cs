using NightOcean.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_SelectableList<T, U> : UIDrawerBase where T : IUIData where U : UIDataDrawerBase<T>
{
    private const float ScrollBarThickness = 20f;
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

    private int rowLimit = -1;
    public int RowLimit
    {
        get => rowLimit;
        set
        {
            rowLimit = value;
            LayoutSizeChanged = true;
        }
    }
    private int columnLimit = -1;
    public int ColumnLimit
    {
        get => columnLimit;
        set
        {
            columnLimit = value;
            LayoutSizeChanged = true;
        }
    }

    private bool horizontalScroll = false;
    public bool HorizontalScroll
    {
        get => horizontalScroll;
        set
        {
            horizontalScroll = value;
            LayoutSizeChanged = true;
        }
    }

    private bool showScrollBar = true;
    public bool ShowScrollBar
    {
        get => showScrollBar;
        set
        {
            showScrollBar = value;
            LayoutSizeChanged = true;
        }
    }

    private ScrollLayoutStrategy layoutStrategy = ScrollLayoutStrategy.ViewGiven;
    public ScrollLayoutStrategy LayoutStrategy
    {
        get => layoutStrategy;
        set
        {
            layoutStrategy = value;
            LayoutSizeChanged = true;
        }
    }

    protected bool LayoutSizeChanged { get; set; } = true;

    protected Vector2 scrollPosition = Vector2.zero;
    protected bool onSelecting = false;

    private Vector2 outRectSize = new(-1f, -1f);
    protected Vector2 OutRectSize
    {
        get
        {
            if (outRectSize.x < 0f || outRectSize.y < 0f)
            {
                RefreshOutRectSize();
            }

            return outRectSize;
        }
    }
    protected Vector2 entryDrawSize = new(-1f, -1f);
    protected Vector2 EntryDrawSize
    {
        get
        {
            if (outRectSize.x < 0f || outRectSize.y < 0f)
            {
                RefreshEntryDrawSize();
            }

            return outRectSize;
        }
    }


    /// <summary>
    /// int：当前的选择索引，bool：索引是否发生了变化
    /// </summary>
    public EventDispatcher<Action<int, bool>> OnSelectedItem { get; } = new();

    public UIDataDrawer_SelectableList(U drawer, IList<T> drawDatas)
    {
        Drawer = drawer;
        DrawDatas = drawDatas;
        LayoutSizeChanged = true;
    }

    public virtual void SetDrawer(U drawer)
    {
        Drawer = drawer;
        LayoutSizeChanged = true;
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

    private void RefreshOutRectSize()
    {
        Vector2 newOutRectSize = ValidDrawSize;
        if (LayoutStrategy != ScrollLayoutStrategy.ViewDerivedByRowCol)
        {
            outRectSize = newOutRectSize;
            return;
        }
        else
        {
            Vector2 entrySize = Drawer.DrawSize;
            float outlineThickness = Drawer.OutlineThickness;
            float stepX = entrySize.x - outlineThickness;
            float stepY = entrySize.y - outlineThickness;

            if (stepX <= 0f || stepY <= 0f)
            {
                outRectSize = newOutRectSize;
                return;
            }

            if (ColumnLimit > 0)
                newOutRectSize.x = ColumnLimit * stepX + outlineThickness;
            if (RowLimit > 0)
                newOutRectSize.y = RowLimit * stepY + outlineThickness;

            Rect viewRect = GetViewRect();
            if (ShowScrollBar)
            {
                if (HorizontalScroll)
                {
                    if (viewRect.width > newOutRectSize.x)
                    {
                        newOutRectSize.y += ScrollBarThickness;
                    }
                }
                else
                {
                    if (viewRect.height > newOutRectSize.y)
                    {
                        newOutRectSize.x += ScrollBarThickness;
                    }
                }
            }

            if (ShowScrollBar)
            {
                if (HorizontalScroll)
                    newOutRectSize.y = Mathf.Max(newOutRectSize.y, ScrollBarThickness);
                else
                    newOutRectSize.x = Mathf.Max(newOutRectSize.x, ScrollBarThickness);
            }
            else
            {
                newOutRectSize.x = Mathf.Max(newOutRectSize.x, 0f);
                newOutRectSize.y = Mathf.Max(newOutRectSize.y, 0f);
            }

            outRectSize = newOutRectSize;
        }
    }

    public void RefreshEntryDrawSize()
    {
        if (LayoutStrategy != ScrollLayoutStrategy.ViewGivenItemAdapt)
            entryDrawSize = Drawer?.DrawSize ?? Vector2.zero;

        if (Drawer is null)
            entryDrawSize = Vector2.zero;

        Vector2 validOutRectSize = ValidDrawSize;
        if (ShowScrollBar)
        {
            if (HorizontalScroll)
                validOutRectSize.y -= ScrollBarThickness;
            else
                validOutRectSize.x -= ScrollBarThickness;
        }

        if (validOutRectSize.x < 0f || validOutRectSize.y < 0f)
            entryDrawSize = Vector2.zero;

        if (HorizontalScroll)
        {
            if (RowLimit > 0)
            {
                float enrtyHeight = validOutRectSize.y / RowLimit;
                Drawer.SetDrawSizeAspectFit(new(validOutRectSize.x, enrtyHeight));
            }
        }
        else
        {
            if (ColumnLimit > 0)
            {
                float enrtyWidth = validOutRectSize.x / ColumnLimit;
                Drawer.SetDrawSizeAspectFit(new(enrtyWidth, validOutRectSize.y));
            }
        }

        entryDrawSize = Drawer.DrawSize;
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
        if (HorizontalScroll)
        {
            totalRows = RowLimit > 0 ? Math.Min(RowLimit, totalCount) : totalCount;
            totalCols = Mathf.CeilToInt((float)totalCount / totalRows);
        }
        else
        {
            totalCols = ColumnLimit > 0 ? Math.Min(ColumnLimit, totalCount) : totalCount;
            totalRows = Mathf.CeilToInt((float)totalCount / totalCols);
        }

        float totalWidth = Mathf.Max(1e-6f, totalCols * stepX + outlineThickness);
        float totalHeight = Mathf.Max(1e-6f, totalRows * stepY + outlineThickness);

        return new Rect(0f, 0f, totalWidth, totalHeight);
    }

    public void Draw(Vector2 position)
    {
        if (Drawer is null || DrawDatas is null || DrawDatas.Count == 0)
            return;

        if (LayoutSizeChanged)
        {
            ResetLayoutSize();
            RefreshLayoutSize();
        }

        Rect outRect = new(position, outRectSize);
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
            if (HorizontalScroll)
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
            else
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
        }
    }

    protected void RefreshLayoutSize()
    {
        RefreshOutRectSize();
        RefreshEntryDrawSize();
        LayoutSizeChanged = false;
    }

    public void ResetLayoutSize()
    {
        LayoutSizeChanged = true;
        outRectSize = new(-1f, -1f);
        entryDrawSize = new(-1f, -1f);
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