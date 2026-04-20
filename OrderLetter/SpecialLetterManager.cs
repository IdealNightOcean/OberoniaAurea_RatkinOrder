using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 特殊信件管理器 - 负责管理骑士团的特殊信件，包括特定游戏事件触发的信件和特定日期触发的信件
/// </summary>
public class SpecialLetterManager : IExposable
{
    protected HashSet<SpecialGameLetterDef> recievedSpecialLetters = [];
    protected HashSet<CertainDateLetterDef> recievedCertainDateLetters = [];

    private int curYear = 2025;

    internal SpecialLetterManager(bool initCtor)
    {
        if (initCtor)
        {
            curYear = DateTime.Now.Year;
        }
    }

    public void Notify_GameStart()
    {
        foreach (SpecialGameLetterDef letterDef in DefDatabase<SpecialGameLetterDef>.AllDefs)
        {
            if (!recievedSpecialLetters.Contains(letterDef))
            {
                OrderLetter orderLetter = OrderLetterUtility.MakeSpecialLetter(letterDef);
                OrderLetterBox.Instance.ReceiveLetter(orderLetter);
                recievedSpecialLetters.Add(letterDef);
            }
        }

        DateTime todayDate = DateTime.Now.Date;

        if (curYear != todayDate.Year)
        {
            ResetAllFestivalLetters();
            curYear = todayDate.Year;
        }

        foreach (CertainDateLetterDef letterDef in DefDatabase<CertainDateLetterDef>.AllDefs)
        {
            if (!recievedCertainDateLetters.Contains(letterDef) && todayDate >= letterDef.EarliestDate && todayDate <= letterDef.LatestDate)
            {
                OrderLetter orderLetter = OrderLetterUtility.MakeSpecialLetter(letterDef);
                OrderLetterBox.Instance.ReceiveLetter(orderLetter);
                recievedCertainDateLetters.Add(letterDef);
            }
        }
    }

    private void ResetAllFestivalLetters()
    {
        recievedCertainDateLetters.Clear();
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref curYear, nameof(curYear), 2025);

        Scribe_Collections.Look(ref recievedSpecialLetters, nameof(recievedSpecialLetters), LookMode.Def);
        Scribe_Collections.Look(ref recievedCertainDateLetters, nameof(recievedCertainDateLetters), LookMode.Def);

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            recievedSpecialLetters.Remove(null);
            recievedCertainDateLetters.Remove(null);
        }
    }
}