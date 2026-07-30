using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class OrderWindowBase : Verse.Window, IUIDrawer
{
    protected override float Margin => 0f;
    protected bool HasClosed { get; set; }

    protected Vector2? sizeOverride;
    public Vector2 DefaultSize => InitialSize;
    public Vector2 DrawSize => sizeOverride ?? InitialSize;

    public TextStyle TextStyle { get; protected set; } = TextStyle.DefaultStyle;

    public OrderWindowBase()
    {
        forcePause = true;
        draggable = false;
        resizeable = false;
        doCloseButton = false;
        doCloseX = false;

        layer = WindowLayer.Dialog;  //窗体层级
        doWindowBackground = false; //绘制泰南的界面背景
        drawShadow = false; //绘制主体界面阴影

        //声音
        //注：用的通讯台声音
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;
    }

    public void SetDrawSize(Vector2 size) => sizeOverride = size;

    public override void Close(bool doCloseSound = true)
    {
        HasClosed = true;
        OAFrame_UIUtility.ResetTextStyleToDefault();
        base.Close(doCloseSound);
    }
}
