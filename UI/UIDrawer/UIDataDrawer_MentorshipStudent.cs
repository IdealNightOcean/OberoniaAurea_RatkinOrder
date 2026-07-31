using NightOcean.Utility;
using OberoniaAurea_Frame.DataLibrary;
using OberoniaAurea_Frame.UI;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_MentorshipStudent : UIDataDrawerBase<UIData_MentorshipStudent>
{
    public override Vector2 DefaultSize => new(328f, 114f);

    public override void DrawInner(Vector2 position)
    {
        Rect boxRect = new(position, DrawSize);
        Rect innerBoxRect = GenUI.ContractedBy(boxRect, 4f); //标准大约(320f,106f);

        Rect portraitRect = new(0f, 0f, innerBoxRect.width * 0.25f, innerBoxRect.height * 0.7f);
        portraitRect = portraitRect.MoveTo(innerBoxRect.TopLeftCorner());
        if (DrawDataValid)
            GUI.DrawTexture(position: portraitRect, image: PortraitsCache.Get(DrawData.Student.Pawn, portraitRect.size, Rot4.South));
        else
            throw new NotImplementedException();

        Rect nameRect = innerBoxRect.BottomPart(0.3f);
        nameRect.width *= 0.3f;
        this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(
            rect: nameRect,
            label: DrawDataValid ? DrawData.Student.Pawn.NameShortColored : "OARO_MentorshipStudent_UnkownStudent".Translate(),
            textStyle: this.TextStyle);

        Rect relationRect = nameRect.MoveTo(nameRect.xMax, nameRect.yMin);
        if (DrawDataValid)
        {
            this.TextStyle = new(guiColor: DrawData.RelationBetweenEach.s2t > 0 ? Color.green : ColorLibrary.RedReadable,
                                 font: GameFont.Medium, anchor: TextAnchor.MiddleLeft);
            OAFrame_Widgets.DrawLabel(rect: relationRect,
                                      label: $"{DrawData.RelationBetweenEach.s2t.ToStringWithSign()} ({DrawData.RelationBetweenEach.t2s.ToStringWithSign()})",
                                      textStyle: this.TextStyle);
        }
        else
        {
            this.TextStyle = new(guiColor: Color.gray, font: GameFont.Medium, anchor: TextAnchor.MiddleLeft);
            OAFrame_Widgets.DrawLabel(relationRect, "0 (0)", this.TextStyle);
        }

        Rect infoRect = innerBoxRect.RightPart(0.65f);
        infoRect.yMax = portraitRect.yMax;

        Rect taughtableCountRect = infoRect.TopHalf();
        Rect dailyTutoringSuccessChanceRect = infoRect.TopHalf();
        this.TextStyle = new(GameFont.Medium, anchor: TextAnchor.MiddleLeft);
        if (DrawDataValid)
        {
            OAFrame_Widgets.DrawLabel(
                rect: taughtableCountRect,
                label: "OARO_MentorshipStudent_TaughtableCountN".Translate(),
                textStyle: this.TextStyle);
            OAFrame_Widgets.DrawLabel(
                rect: dailyTutoringSuccessChanceRect,
                label: "OARO_MentorshipStudent_DailyTutoringSuccessChance".Translate(
                    0f.ToStringPercent()
                      .Colorize(Color.gray)
                      .Named(KeyLibrary_FormatArgName.Chance)),
                textStyle: this.TextStyle);
        }
        else
        {
            OAFrame_Widgets.DrawLabel(
                rect: taughtableCountRect,
                label: "OARO_MentorshipStudent_TaughtableCount".Translate(
                    DrawData.TaughtableAcademicsCount.ToString()
                                                     .Colorize(Color.green)
                                                     .Named(KeyLibrary_FormatArgName.Count)),
                textStyle: this.TextStyle);
            OAFrame_Widgets.DrawLabel(
                rect: dailyTutoringSuccessChanceRect,
                label: "OARO_MentorshipStudent_DailyTutoringSuccessChance".Translate(
                    DrawData.DailyTutoringSuccessChance.ToStringPercent()
                                                       .Colorize(Color.green)
                                                       .Named(KeyLibrary_FormatArgName.Chance)),
                textStyle: this.TextStyle);
        }
    }
}
