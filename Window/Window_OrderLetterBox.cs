using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_OrderLetterBox : Window
{
    //主体间隙
    //注：默认DoWindowContents不填满inRect的内容，需要去除
    protected override float Margin => 0.0f;
    //主体界面大小
    public override Vector2 InitialSize => new(1080, 760);

    ////// 贴图
    //// 其它
    //信件类型

    //// 主体
    //背景
    public static readonly Texture2D Texture_Main_Background = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_MainBackground", true);

    //// 主体 - 左上
    //背景
    public static readonly Texture2D Texture_TopLeft_Background = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_AMainBackground", true);
    //信件 - 装饰 - 关
    public static readonly Texture2D Texture_TopLeft_Letter_Close = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_LetterClose", true);
    //信件 - 装饰 - 开
    public static readonly Texture2D Texture_TopLeft_Letter_Open = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_LetterOpen", true);

    //信件 - 列表
    public static readonly Texture2D Texture_TopLeft_LetterList_Background = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_AListBackground", true);
    //信件 - 列表 - 背景
    public static readonly Texture2D Texture_TopLeft_LetterList_SelectedBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_ASelectedBackground", true);

    //// 主体 - 右上
    //背景
    public static readonly Texture2D Texture_TopRight_Background = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_DMainBackground", true);

    public static readonly Texture2D Texture_TopRight_TitleBottomLine = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_DTitleBottomLine", true);
    //详细 - 背景
    public static readonly Texture2D Texture_TopRight_MainBody_Background = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_DBodyBackground", true);
    //详细 - 背景 - 装饰 - A
    public static readonly Texture2D Texture_TopRight_MainBody_Background_Add_A = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_DDecorative", true);

    //// 主体 - 左下
    //背景
    public static readonly Texture2D Texture_BottomLeft_Background = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_CMainBackground", true);

    //// 主体 - 右下
    //背景
    public static readonly Texture2D Texture_BottomRight_Background = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_EMainBackground", true);
    //装饰 - 信件 - 左侧
    public static readonly Texture2D Texture_BottomRight_LetterAdd_Left = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_EDecorativeLeft", true);
    //装饰 - 信件 - 右侧
    public static readonly Texture2D Texture_BottomRight_LetterAdd_Right = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_EDecorativeRight", true);

    //// 工具 - 按钮
    //通用背景
    public static readonly Texture2D Texture_ToolBar_Button_Background = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_ButtonBackground", true);
    //关闭
    public static readonly Texture2D Texture_ToolBar_Button_Close = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_Close", true);
    //设置
    public static readonly Texture2D Texture_ToolBar_Button_Setting = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_UI_Setting", true);


    ////变量
    //选中信件
    //注：
    //  -1表示默认没有选中
    //  给可能出现的外部调用/修改

    public int selectedLetterIndex = -1;
    private readonly OrderLetterBox LetterBox;
    private OrderLetter curLetter = null; // 当前选中的信件
    private string curLetterDesc;

    //滚动条 - 信封列表
    private Vector2 scrollPosition_letterList = Vector2.zero;

    private List<OrderLetter> archivedLetters = [];

    //构造函数
    public Window_OrderLetterBox() : base()
    {
        //开启界面暂停
        forcePause = true;
        //是否可以拖动
        draggable = false;
        //是否可以改变大小
        resizeable = false;
        //绘制泰南自己的关闭按钮
        doCloseButton = false;
        //同上
        doCloseX = false;
        //窗体层级
        layer = WindowLayer.Dialog;
        //绘制泰南的界面背景
        doWindowBackground = false;
        //绘制主体界面阴影
        drawShadow = false;

        //声音
        //注：用的通讯台声音
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;

        LetterBox = OrderLetterBox.Instance;

        archivedLetters = LetterBox.ArchivedLetters;
        curLetter = null;
        curLetterDesc = GetEmptyLetterDesc(); // 没有选中信件时的描述
    }

    //绘制
    public override void DoWindowContents(Rect inRect)
    {
        //获取主体
        Rect R_Main = inRect;

        //绘制主体背景
        GUI.DrawTexture(R_Main, Texture_Main_Background);

        //主体显示部分与主体外框的间隙
        float float_MainBodyPart_Margin = 25.0f;

        //左侧主体部分
        Rect R_MainBodyPart = new()
        {
            x = R_Main.x + float_MainBodyPart_Margin,
            y = R_Main.y + float_MainBodyPart_Margin,
            width = R_Main.width - float_MainBodyPart_Margin * 3,
            height = R_Main.height - float_MainBodyPart_Margin * 2
        };
        {
            //左上角
            Rect R_MainBodyPart_TopLeft = new()
            {
                x = R_MainBodyPart.x,
                y = R_MainBodyPart.y,
                width = R_MainBodyPart.width / 8 * 3,
                height = R_MainBodyPart.height / 4 * 3
            };
            {
                //四部分的间隔。以下同理
                Rect R_TopLeft_MainBodyPart = R_MainBodyPart_TopLeft.ContractedBy(5.0f);
                {
                    //背景贴图，以下同理
                    GUI.DrawTexture(R_TopLeft_MainBodyPart, Texture_TopLeft_Background);

                    ////部分的内容，以下同理
                    //上半部分的信件显示区域
                    Rect R_MainBodyPart_TopPart = new()
                    {
                        x = R_TopLeft_MainBodyPart.x,
                        y = R_TopLeft_MainBodyPart.y,
                        width = R_TopLeft_MainBodyPart.width,
                        height = R_TopLeft_MainBodyPart.height / 4
                    };
                    {
                        float float_TopPart_DrawPart_TBMargin = 10.0f;
                        float target_DrawPart_Width = R_MainBodyPart_TopPart.height - float_TopPart_DrawPart_TBMargin * 2;
                        float target_DrawPart_Height = target_DrawPart_Width / 3 * 2;
                        Rect Rect_TopPart_DrawPart = new()
                        {
                            height = target_DrawPart_Height,
                            width = target_DrawPart_Width,
                            x = R_MainBodyPart_TopPart.x + (R_MainBodyPart_TopPart.width / 2) - (target_DrawPart_Width / 2),
                            y = R_MainBodyPart_TopPart.y + (R_MainBodyPart_TopPart.height / 2) - (target_DrawPart_Width / 2),
                        };
                        {
                            if (true) /* ! - 这里是信件贴图开关样式判断条件 - ! */
                            {
                                GUI.DrawTexture(Rect_TopPart_DrawPart, Texture_TopLeft_Letter_Close);
                            }
                            else
                            {
                                GUI.DrawTexture(Rect_TopPart_DrawPart, Texture_TopLeft_Letter_Open);
                            }
                        }
                    }

                    //下半部分的信件列表
                    Rect R_MainBodyPart_BottomPart = new()
                    {
                        x = R_TopLeft_MainBodyPart.x,
                        y = R_TopLeft_MainBodyPart.y + R_MainBodyPart_TopPart.height - 20.0f /* 手动偏移量 */,
                        width = R_TopLeft_MainBodyPart.width,
                        height = R_TopLeft_MainBodyPart.height / 4 * 3
                    };
                    {
                        //列表显示范围
                        Rect Rect_TopPart_LetterListPart = R_MainBodyPart_BottomPart.ContractedBy(10.0f);
                        {
                            //列表外框
                            GUI.DrawTexture(Rect_TopPart_LetterListPart, Texture_TopLeft_LetterList_Background);

                            //限制信封列表高度
                            float float_LetterList_Hieght = 60.0f;
                            //滚动条显示范围间隙
                            float float_letterListScroll_Margin = 5.0f;

                            //列表的滚动实际范围
                            Rect R_LetterListScrollPart = new()
                            {
                                x = Rect_TopPart_LetterListPart.x + float_letterListScroll_Margin,
                                y = Rect_TopPart_LetterListPart.y + float_letterListScroll_Margin,
                                width = Rect_TopPart_LetterListPart.width - float_letterListScroll_Margin * 2,
                                height = archivedLetters.Count * float_LetterList_Hieght >= Rect_TopPart_LetterListPart.height
                                         ? archivedLetters.Count * float_LetterList_Hieght
                                         : archivedLetters.Count * float_LetterList_Hieght - float_letterListScroll_Margin * 2
                            };
                            {
                                //滚动条
                                Widgets.BeginScrollView(Rect_TopPart_LetterListPart, ref scrollPosition_letterList, R_LetterListScrollPart, false);
                                {
                                    ////获取 与 绘制信件列表
                                    for (int i = LetterBox.ArchivedLettersCount - 1; i >= 0; i--)
                                    {
                                        OrderLetter letter = LetterBox.ArchivedLetters[i];
                                        Rect R_Letter = new()
                                        {
                                            x = R_LetterListScrollPart.x,
                                            y = R_LetterListScrollPart.y + i * float_LetterList_Hieght,
                                            width = R_LetterListScrollPart.width,
                                            height = float_LetterList_Hieght
                                        };
                                        {
                                            //信封贴图显示间隔
                                            float float_LetterIcon_Margin = 10.0f;
                                            //前端的信件类型绘制
                                            Rect R_Letter_Icon = new()
                                            {
                                                x = R_Letter.x + float_LetterIcon_Margin,
                                                y = R_Letter.y + float_LetterIcon_Margin,
                                                width = (R_Letter.height - float_LetterIcon_Margin * 2) * 1.3f /*手动调整偏移量*/,
                                                height = R_Letter.height - float_LetterIcon_Margin * 2
                                            };

                                            //后面的信件内容绘制
                                            Rect R_Letter_Title = new()
                                            {
                                                x = R_Letter.x + R_Letter_Icon.width + 15.0f/* 手动偏移量 */,
                                                y = R_Letter.y,
                                                width = R_Letter.width - R_Letter_Icon.width - 15.0f/* 补偿手动偏移量 */,
                                                height = R_Letter.height
                                            };

                                            //按钮的hover与点击
                                            if (LetterButtonImage(R_Letter, selectedLetterIndex == i))
                                            {
                                                ////点击事件
                                                //选中
                                                if (selectedLetterIndex == i)
                                                {
                                                    selectedLetterIndex = -1;
                                                    curLetter = null;
                                                    curLetterDesc = GetEmptyLetterDesc(); // 没有选中信件时的描述
                                                }
                                                else
                                                {
                                                    selectedLetterIndex = i;
                                                    curLetter = letter;
                                                    curLetterDesc = letter.GetLetterDesc(); // 获取选中信件的描述
                                                }
                                            }

                                            //由于先后绘制关系，我需要在显示icon与标题之前绘制选择与hover的贴图
                                            {
                                                GUI.DrawTexture(R_Letter_Icon, letter.Icon);
                                                Text.Anchor = TextAnchor.MiddleLeft;
                                                Widgets.Label(R_Letter_Title, letter.Label);
                                                ResetTextFont();
                                            }
                                        }
                                    }
                                }
                                Widgets.EndScrollView();
                            }

                        }
                    }
                }
            }

            //右上角
            Rect R_MainBodyPart_TopRight = new()
            {
                x = R_MainBodyPart.x + R_MainBodyPart_TopLeft.width,
                y = R_MainBodyPart.y,
                width = R_MainBodyPart.width / 8 * 5,
                height = R_MainBodyPart.height / 4 * 3
            };
            {
                Rect R_TopRight_MainBodyPart = R_MainBodyPart_TopRight.ContractedBy(5.0f);
                {
                    GUI.DrawTexture(R_TopRight_MainBodyPart, Texture_TopRight_Background);

                    //顶部信封类型与标题部分
                    Rect R_MainBodyPart_TopPart = new()
                    {
                        x = R_TopRight_MainBodyPart.x,
                        y = R_TopRight_MainBodyPart.y,
                        width = R_TopRight_MainBodyPart.width,
                        height = R_TopRight_MainBodyPart.height / 6
                    };


                    float float_TitleLRMargin = 20.0f;
                    Rect R_TopPart_Title = new()
                    {
                        x = R_MainBodyPart_TopPart.x + float_TitleLRMargin,
                        y = R_MainBodyPart_TopPart.y + 10.0f /* 手动偏移量 */,
                        width = R_MainBodyPart_TopPart.width - float_TitleLRMargin * 2,
                        height = R_MainBodyPart_TopPart.height
                    };
                    {
                        //信封贴图显示间隔
                        float float_TitleIcon_Margin = 20.0f;
                        float title_Icon_Width = (R_TopPart_Title.height - float_TitleIcon_Margin * 2) * 1.3f; /*手动调整偏移量*/
                        if (curLetter is not null)
                        {
                            //信封类型绘制
                            Rect R_Title_Icon = new Rect()
                            {
                                x = R_TopPart_Title.x + float_TitleIcon_Margin,
                                y = R_TopPart_Title.y + float_TitleIcon_Margin,
                                width = title_Icon_Width,
                                height = R_TopPart_Title.height - 2 * float_TitleIcon_Margin
                            }.CenteredOnYIn(R_TopPart_Title);
                            {
                                GUI.DrawTexture(R_Title_Icon, curLetter.Icon);
                            }
                            //标题位置
                            Rect R_Title_Text = new()
                            {
                                x = R_TopPart_Title.x + R_Title_Icon.width + 30.0f/* 手动偏移量 */,
                                y = R_TopPart_Title.y,
                                width = R_TopPart_Title.width - R_Title_Icon.width - 30.0f/* 补偿手动偏移量 */,
                                height = R_TopPart_Title.height * 5 / 6f
                            };
                            {
                                Text.Font = GameFont.Medium;
                                Text.Anchor = TextAnchor.MiddleLeft;
                                Widgets.Label(R_Title_Text, curLetter.Label);
                                ResetTextFont();
                            }
                        }
                        //标题底部线条
                        Rect R_Title_BottomLine = Rect.MinMaxRect(R_TopPart_Title.x + title_Icon_Width + 30.0f, R_TopPart_Title.yMax - R_TopPart_Title.height / 6f, R_TopPart_Title.xMax - 30f, R_TopPart_Title.yMax);
                        {
                            GUI.DrawTexture(R_Title_BottomLine, Texture_TopRight_TitleBottomLine);
                        }
                    }

                    //底部信封内容部分
                    Rect R_MainBodyPart_BottomPart = new()
                    {
                        x = R_TopRight_MainBodyPart.x,
                        y = R_TopRight_MainBodyPart.y + R_MainBodyPart_TopPart.height,
                        width = R_TopRight_MainBodyPart.width,
                        height = R_TopRight_MainBodyPart.height / 6 * 5
                    };
                    {
                        Rect R_BottomPart_MainBodyPart = R_MainBodyPart_BottomPart.ContractedBy(30.0f);
                        {
                            //背景框
                            GUI.DrawTexture(R_BottomPart_MainBodyPart, Texture_TopRight_MainBody_Background);

                            ////中心花纹
                            //比例固定
                            Vector2 V2_Add_A = new(342.0f, 367.0f);
                            //大小控制
                            float Factor_Add_A = 0.8f;
                            {
                                V2_Add_A.x *= Factor_Add_A;
                                V2_Add_A.y *= Factor_Add_A;
                            }
                            Rect R_BittomPart_Add_A = new()
                            {
                                width = V2_Add_A.x,
                                height = V2_Add_A.y,
                                x = R_BottomPart_MainBodyPart.x + (R_BottomPart_MainBodyPart.width / 2) - (V2_Add_A.x / 2),
                                y = R_BottomPart_MainBodyPart.y + (R_BottomPart_MainBodyPart.height / 2) - (V2_Add_A.y / 2)
                            };
                            {
                                GUI.DrawTexture(R_BittomPart_Add_A, Texture_TopRight_MainBody_Background_Add_A);
                            }

                            //文本显示部分
                            if (curLetter is not null)
                            {
                                Rect R_MainBodyPart_ShowTxt = R_BottomPart_MainBodyPart.ContractedBy(30.0f);
                                {
                                    Text.Font = GameFont.Medium;
                                    Text.Anchor = TextAnchor.UpperCenter;
                                    Widgets.TextArea(R_MainBodyPart_ShowTxt, curLetter.Text, readOnly: true);
                                    ResetTextFont();
                                }
                            }
                        }
                    }
                }
            }

            //左下角
            Rect R_MainBodyPart_BottomLeft = new()
            {
                x = R_MainBodyPart.x,
                y = R_MainBodyPart.y + R_MainBodyPart_TopLeft.height,
                width = R_MainBodyPart.width / 8 * 3,
                height = R_MainBodyPart.height / 4
            };
            {
                Rect R_BottomLeft_MainBodyPart = R_MainBodyPart_BottomLeft.ContractedBy(5.0f);
                {
                    GUI.DrawTexture(R_BottomLeft_MainBodyPart, Texture_BottomLeft_Background);

                    // Text.Font = GameFont.Medium;
                    Text.Anchor = TextAnchor.MiddleCenter;

                    Widgets.Label(R_BottomLeft_MainBodyPart, "敬请期待" /* ! - 这里替换文本 - !*/);

                    ResetTextFont();
                }
            }

            //右下角
            Rect R_MainBodyPart_BottomRight = new()
            {
                x = R_MainBodyPart.x + R_MainBodyPart_TopLeft.width,
                y = R_MainBodyPart.y + R_MainBodyPart_TopLeft.height,
                width = R_MainBodyPart.width / 8 * 5,
                height = R_MainBodyPart.height / 4
            };
            {
                Rect R_BottomRight_MainBodyPart = R_MainBodyPart_BottomRight.ContractedBy(5.0f);
                {
                    GUI.DrawTexture(R_BottomRight_MainBodyPart, Texture_BottomRight_Background);

                    //文本显示
                    /* ! - 这里是右下角的文本显示区，为了文本与装饰贴图的美观，请务必写在这个位置 - ！*/
                    Rect R_BottomRight_LetterHead = new()
                    {
                        x = R_BottomRight_MainBodyPart.x + R_BottomRight_MainBodyPart.width / 4f,
                        y = R_BottomRight_MainBodyPart.y + R_BottomRight_MainBodyPart.height / 5f,
                        width = R_BottomRight_MainBodyPart.width / 2f,
                        height = R_BottomRight_MainBodyPart.height * 0.6f
                    };
                    {
                        Text.Font = GameFont.Medium;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.TextArea(R_BottomRight_LetterHead, curLetterDesc, readOnly: true);
                        ResetTextFont(); // 重置字体设置
                    }

                    ////装饰
                    Rect R_BottomRight_LetterAdd = R_BottomRight_MainBodyPart.ContractedBy(20.0f);
                    //左侧装饰
                    Rect R_BottomRight_LetterAdd_Left = new()
                    {
                        x = R_BottomRight_LetterAdd.x,
                        y = R_BottomRight_LetterAdd.y,
                        width = R_BottomRight_LetterAdd.height,
                        height = R_BottomRight_LetterAdd.height
                    };
                    {
                        GUI.DrawTexture(R_BottomRight_LetterAdd_Left, Texture_BottomRight_LetterAdd_Left);
                    }

                    //右侧装饰
                    Rect R_BottomRight_LetterAdd_Right = new()
                    {
                        x = R_BottomRight_LetterAdd.x + R_BottomRight_LetterAdd.width - R_BottomRight_LetterAdd.height,
                        y = R_BottomRight_LetterAdd.y,
                        width = R_BottomRight_LetterAdd.height,
                        height = R_BottomRight_LetterAdd.height
                    };
                    {
                        GUI.DrawTexture(R_BottomRight_LetterAdd_Right, Texture_BottomRight_LetterAdd_Right);
                    }
                }
            }
        }

        //工具栏从示例图来看是间隙两倍左右
        float float_ToolBar_Width = float_MainBodyPart_Margin * 2;

        //右侧 关闭按钮 与 设置按钮 的绘制界面
        Rect R_ToolBar_Part = new()
        {
            x = R_Main.x + R_Main.width - float_ToolBar_Width - 7.0f/*手动偏移量*/,
            y = R_Main.y + float_MainBodyPart_Margin,
            width = float_ToolBar_Width,
            height = R_Main.height - float_ToolBar_Width
        };
        {
            //按钮的左右间隔
            float float_ToolBarButton_LRMargin = 8.0f;
            //按钮的上下间隔
            float float_ToolBarButton_TBMargin = 8.0f;

            //获取右侧工具栏按钮列表
            IEnumerable<Button_OrderLetter_ToolBar> list_Button_OrderLetter_ToolBar = Get_Button_OrderLetter_ToolBar();
            //绘制工具栏按钮
            for (int i = 0; i < list_Button_OrderLetter_ToolBar.Count(); i++)
            {
                //获取当前按钮
                Button_OrderLetter_ToolBar currentButton = list_Button_OrderLetter_ToolBar.ElementAt(i);

                //按钮矩形
                Rect R_ToolBar_Button = new()
                {
                    x = R_ToolBar_Part.x + float_ToolBarButton_LRMargin,
                    y = R_ToolBar_Part.y /*起点*/ + i * ((R_ToolBar_Part.width - float_ToolBarButton_LRMargin * 2) + float_ToolBarButton_TBMargin) /*数量 x 上一个与间隔*/,
                    width = R_ToolBar_Part.width - float_ToolBarButton_LRMargin * 2,
                    height = R_ToolBar_Part.width - float_ToolBarButton_LRMargin * 2
                };
                {
                    //绘制按钮背景
                    GUI.DrawTexture(R_ToolBar_Button, Texture_ToolBar_Button_Background);

                    Rect R_ToolBar_Button_Icon = R_ToolBar_Button.ContractedBy(6.0f);
                    {
                        GUI.DrawTexture(R_ToolBar_Button_Icon, currentButton.icon);
                    }

                    //点击事件 与 按钮图标 与
                    if (Widgets.ButtonInvisible(R_ToolBar_Button, true))
                    {
                        currentButton.action_Click?.Invoke();
                    }
                }
            }
        }
    }

    //获取右侧工具栏按钮列表
    public IEnumerable<Button_OrderLetter_ToolBar> Get_Button_OrderLetter_ToolBar()
    {
        //关闭按钮
        yield return new Button_OrderLetter_ToolBar
            (
                icon: Texture_ToolBar_Button_Close,
                action_Click: () => Close()
            );
        //设置按钮
        yield return new Button_OrderLetter_ToolBar
            (
                icon: Texture_ToolBar_Button_Setting,
                action_Click: () => Find.WindowStack.Add(new Window_LetterBoxSetting())
            );
    }

    /// <summary>
    /// 信封专用按钮绘制方法
    /// 根据鼠标是否悬停或选中来绘制不同的贴图
    /// </summary>
    private static bool LetterButtonImage(Rect buttonRect, bool selected)
    {
        if (selected)
        {
            Widgets.DrawStrongHighlight(buttonRect, Color.cyan);
            Widgets.DrawBox(buttonRect);
        }
        else if (Mouse.IsOver(buttonRect))
        {
            Widgets.DrawHighlight(buttonRect);
        }
        return Widgets.ButtonInvisible(buttonRect);
    }

    private static string GetEmptyLetterDesc()
    {
        StringBuilder sb = new("OARO_Letter_LetterSender".Translate());
        sb.Append("OARO_Letter_UnkownSender".Translate());
        sb.AppendInNewLine("OARO_Letter_RelatedOrder".Translate());
        sb.Append("None".Translate());
        sb.AppendInNewLine("OARO_Letter_RelatedThings".Translate());
        sb.Append("None".Translate());
        sb.AppendInNewLine("OARO_Letter_SendTime".Translate());
        sb.Append("None".Translate());
        return sb.ToString();
    }

    //重置font
    private static void ResetTextFont()
    {
        //重置
        GUI.color = Color.white;
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperLeft;
    }
}

public class Button_OrderLetter_ToolBar
{
    //工具栏按钮的图标
    //注：
    //  外框背景用的工具栏按钮通用外框
    //  所以这里只要写图标
    public Texture2D icon;

    //工具栏按钮的点击事件
    public Action action_Click;

    public Button_OrderLetter_ToolBar(Texture2D icon, Action action_Click)
    {
        this.icon = icon;
        this.action_Click = action_Click;
    }
}