using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderLetterBox : IExposable
{
    public static OrderLetterBox Instance { get; private set; }

    public const int MaxLetterCount = 100;
    public const int MaxLetterRetentionDays = 60;
    public bool HasMaxLetterLimit = false;

    public bool autoTransNormal;
    public bool autoTransUrgent = true;
    public bool autoTransOfficial = true;

    [Unsaved] private float nextCanClickTime = -1;

    protected List<OrderLetter> unreadLetters = []; // 未读邮件
    protected List<OrderLetter> archivedLetters = []; // 已读邮件

    public SpecialLetterManager specialLetterManager;

    public List<OrderLetter> ArchivedLetters => archivedLetters;

    public bool HasUnreadLetters => unreadLetters.Count > 0;
    public int ArchivedLettersCount => archivedLetters.Count;

    public OrderLetterBox()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
        specialLetterManager = new SpecialLetterManager();
    }
    public static void ClearStaticCache() => Instance = null;

    public void LetterBoxDay()
    {
        archivedLetters.AddRange(unreadLetters.Where(l => l.Expired));
        unreadLetters.RemoveAll(l => l.Expired);

        RemoveOvercapArchivedLetters();
    }
    public void ReceiveLetter(OrderLetter letter)
    {
        letter.ArrivalTick = Find.TickManager.TicksGame;
        unreadLetters.Add(letter);
    }

    public void ReadSingleLetter(OrderLetter letter, Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        try
        {
            OrderLetterUtility.ReadLetter(letter, letterBox, forceSlience);
        }
        catch (Exception ex)
        {
            Log.Error($"Error when reading letter, force archived. {ex.Message}");
        }
        ArchiveLetter(letter);
    }

    public void ReadAllUnreadLetters(Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        try
        {
            foreach (OrderLetter letter in unreadLetters)
            {
                OrderLetterUtility.ReadLetter(letter, letterBox, forceSlience);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error when reading all unread letters, force archived. {ex.Message}");
        }

        archivedLetters.AddRange(unreadLetters);
        unreadLetters.Clear();
        RemoveOvercapArchivedLetters();
    }

    public void ClearAllUnreadLetters()
    {
        archivedLetters.AddRange(unreadLetters);
        unreadLetters.Clear();
        RemoveOvercapArchivedLetters();
    }

    public void ClearAllArchivedLetters()
    {
        archivedLetters.Clear();
    }

    public bool CanTriggerDialog()
    {
        if (Time.time > nextCanClickTime)
        {
            return true;
        }
        return false;
    }

    private void RemoveOvercapArchivedLetters()
    {
        if (HasMaxLetterLimit)
        {
            int overCap = archivedLetters.Count - MaxLetterCount;
            if (overCap > 0)
            {
                archivedLetters.SortBy(l => l.ArrivalTick);
                archivedLetters.RemoveRange(0, overCap);
            }
        }
    }

    private void ArchiveLetter(OrderLetter letter)
    {
        unreadLetters.Remove(letter);
        archivedLetters.Add(letter);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref autoTransNormal, "autoTransNormal", defaultValue: false);
        Scribe_Values.Look(ref autoTransOfficial, "autoTransOfficial", defaultValue: true);
        Scribe_Values.Look(ref autoTransUrgent, "autoTransUrgent", defaultValue: true);

        Scribe_Deep.Look(ref specialLetterManager, "specialLetterManager");

        Scribe_Collections.Look(ref unreadLetters, "unreadLetters", LookMode.Deep);
        Scribe_Collections.Look(ref archivedLetters, "archivedLetters", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            unreadLetters.RemoveAll(l => l is null);
            archivedLetters.RemoveAll(l => l is null);
        }
    }

}
