using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class OrderWindowBase : Verse.Window, IUIDrawer
{
    protected override float Margin => 0f;
    protected bool HasClosed { get; set; }

    public Vector2 InitSize => InitialSize;

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

    public override void Close(bool doCloseSound = true)
    {
        HasClosed = true;
        OAFrame_UIUtility.ResetTextStyleToDefault();
        base.Close(doCloseSound);
    }
}
