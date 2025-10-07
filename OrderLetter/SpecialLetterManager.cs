using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SpecialLetterManager : IExposable
{
    protected HashSet<SpecialLetterDef> recievedSpecialLetters = [];
    protected HashSet<CertainDateLetterDef> recievedCertainDateLetters = [];

    private int curYear = 2025;

    public void Notify_GameStart()
    {
        foreach (SpecialLetterDef letterDef in DefDatabase<SpecialLetterDef>.AllDefs)
        {
            if (!recievedSpecialLetters.Contains(letterDef))
            {
                OrderLetter specialLetter = OrderLetterUtility.MakeOrderLetter(letterDef.label, letterDef.text, letterDef.letterType, sender: letterDef.sender, relatedOrder: null);
                OrderLetterBox.Instance.ReceiveLetter(specialLetter);
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
                OrderLetter certainDateLetter = OrderLetterUtility.MakeOrderLetter(letterDef.label, letterDef.text, letterDef.letterType, sender: letterDef.sender, relatedOrder: null);
                OrderLetterBox.Instance.ReceiveLetter(certainDateLetter);
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
        Scribe_Values.Look(ref curYear, "curYear", 2025);

        Scribe_Collections.Look(ref recievedSpecialLetters, "recievedSpecialLetters", LookMode.Def);
        Scribe_Collections.Look(ref recievedCertainDateLetters, "recievedCertainDateLetters", LookMode.Def);

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            recievedCertainDateLetters.RemoveWhere(l => l is null);
            recievedCertainDateLetters.RemoveWhere(l => l is null);
        }
    }
}