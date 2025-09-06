using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SpecialLetterManager : IExposable
{
    private static OrderLetterBox OrderLetterBox => OrderLetterBox.Instance;

    private bool helloLetterReceived;
    protected HashSet<CertainDateLetterDef> recievedCertainDateLetters = [];

    private int curYear = 2025;

    public void Notify_GameStart()
    {
        if (!helloLetterReceived)
        {
            TryMakeHelloLetter();
            helloLetterReceived = true;
        }

        DateTime todayDate = DateTime.Now.Date;

        if (curYear != todayDate.Year)
        {
            ResetAllFestivalLetters();
            curYear = todayDate.Year;
        }

        foreach (CertainDateLetterDef def in DefDatabase<CertainDateLetterDef>.AllDefs)
        {
            if (!recievedCertainDateLetters.Contains(def) && todayDate >= def.EarliestDate && todayDate <= def.LatestDate)
            {
                OrderLetter certainDateLetter = OrderLetterUtility.MakeOrderLetter(def.label, def.text, def.letterType, sender: def.sender, relatedOrder: null);
                OrderLetterBox.ReceiveLetter(certainDateLetter);
                recievedCertainDateLetters.Add(def);
            }
        }
    }

    private void ResetAllFestivalLetters()
    {
        recievedCertainDateLetters.Clear();
    }

    public static void TryMakeHelloLetter()
    {
        OrderLetter helloLetter = OrderLetterUtility.MakeOrderLetter("OARO_LetterLabel_HelloLetter".Translate(), "OARO_Letter_HelloLetter".Translate(), OrderLetterType.Urgent, sender: "OARO_MYG", relatedOrder: null);
        OrderLetterBox.ReceiveLetter(helloLetter);
    }

    public void ExposeData()
    {

        Scribe_Values.Look(ref helloLetterReceived, "helloLetterReceived", defaultValue: false);

        Scribe_Values.Look(ref curYear, "curYear", 2025);

        Scribe_Collections.Look(ref recievedCertainDateLetters, "recievedCertainDateLetters", LookMode.Def);

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            recievedCertainDateLetters.RemoveWhere(d => d is null);
        }
    }
}