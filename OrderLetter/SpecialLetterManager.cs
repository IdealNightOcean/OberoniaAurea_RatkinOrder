using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SpecialLetterManager : IExposable
{
    private static OrderLetterBox LetterBox => OrderLetterBox.Instance;

    private bool helloLetterReceived;

    private int curYear = 2025;
    private bool childFestivalLetterReceived;

    public void Notify_GameStart()
    {
        if (!helloLetterReceived)
        {
            TryMakeHelloLetter();
            helloLetterReceived = true;
        }

        if (curYear < DateTime.Now.Year)
        {
            ResetAllFestivalLetters();
            curYear = DateTime.Now.Year;
        }

        int mouth = DateTime.Now.Month;
        int day = DateTime.Now.Day;
        if (mouth == 6)
        {
            if (!childFestivalLetterReceived && day > 2 && day < 6)
            {
                OrderLetter childFestivalLetter = OrderLetterUtility.MakeOrderLetter("OARO_LetterLabel_ChildFestival".Translate(), "OARO_Letter_ChildFestival".Translate(), OrderLetter.LetterType.Urgent, relatedOrder: null, sender: "OARO_MYG");
                LetterBox.ReceiveLetter(childFestivalLetter);
                childFestivalLetterReceived = true;
            }
        }
    }

    private void ResetAllFestivalLetters()
    {
        childFestivalLetterReceived = false;
    }

    public static void TryMakeHelloLetter()
    {
        OrderLetter helloLetter = OrderLetterUtility.MakeOrderLetter("OARO_LetterLabel_HelloLetter".Translate(), "OARO_Letter_HelloLetter".Translate(), OrderLetter.LetterType.Urgent, relatedOrder: null, sender: "OARO_MYG");
        LetterBox.ReceiveLetter(helloLetter);
    }

    public void ExposeData()
    {

        Scribe_Values.Look(ref helloLetterReceived, "helloLetterReceived", defaultValue: false);

        Scribe_Values.Look(ref curYear, "curYear", 2025);

        Scribe_Values.Look(ref childFestivalLetterReceived, "childFestivalLetterReceivedTemp", defaultValue: false);
    }
}