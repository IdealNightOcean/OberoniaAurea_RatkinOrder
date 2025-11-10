using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderLetterBox : IExposable
{
    public static OrderLetterBox Instance { get; private set; }

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
        List<OrderLetter> validLetters = new(8);
        List<OrderLetter> expiredLetters = new(8);

        for (int i = 0; i < unreadLetters.Count; i++)
        {
            if (unreadLetters[i].Expired)
            {
                expiredLetters.Add(unreadLetters[i]);
            }
            else
            {
                validLetters.Add(unreadLetters[i]);
            }
        }

        if (expiredLetters.Count > 0)
        {
            unreadLetters.Clear();
            unreadLetters.AddRange(validLetters);
            OAFrame_CollectionUtility.MergeSortedListsInplace(archivedLetters, expiredLetters, comparerFunc: OrderLetterComparerFunc);

            if (RatkinOrderSettings.HasMaxLetterLimit)
            {
                RemoveOvercapArchivedLetters();
            }
        }

        if (RatkinOrderSettings.HasLetterRetentionLimit)
        {
            int ticksGame = Find.TickManager.TicksGame;
            int maxLetterRetentionDays = RatkinOrderSettings.MaxLetterRetentionDays;
            archivedLetters.RemoveAll(r => (ticksGame - r.ArrivalTick) / 60000 >= maxLetterRetentionDays);
        }
    }

    public void ReceiveLetter(OrderLetter letter)
    {
        letter.ArrivalTick = Find.TickManager.TicksGame;
        OAFrame_CollectionUtility.BinaryInsertion(unreadLetters, letter, comparerFunc: OrderLetterComparerFunc);
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
            for (int i = 0; i < unreadLetters.Count; i++)
            {
                OrderLetterUtility.ReadLetter(unreadLetters[i], letterBox, forceSlience);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error when reading all unread letters, force archived. {ex.Message}");
        }

        ClearAllUnreadLetters();
    }

    public void ClearAllUnreadLetters()
    {
        OAFrame_CollectionUtility.MergeSortedListsInplace(archivedLetters, unreadLetters, comparerFunc: OrderLetterComparerFunc);
        unreadLetters.Clear();
        if (RatkinOrderSettings.HasMaxLetterLimit)
        {
            RemoveOvercapArchivedLetters();
        }
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
        int overCap = archivedLetters.Count - RatkinOrderSettings.MaxLetterCount;
        if (overCap > 0)
        {
            archivedLetters.RemoveRange(archivedLetters.Count - overCap, overCap);
        }
    }

    private void ArchiveLetter(OrderLetter letter)
    {
        unreadLetters.Remove(letter);
        OAFrame_CollectionUtility.BinaryInsertion(archivedLetters, letter, comparerFunc: OrderLetterComparerFunc);
    }

    /// <summary>
    /// 降序比较方法
    /// </summary>
    private static int OrderLetterComparerFunc(OrderLetter a, OrderLetter b) => b.ArrivalTick.CompareTo(a.ArrivalTick);

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

            unreadLetters.SortBy(r => -r.ArrivalTick);
            archivedLetters.SortBy(r => -r.ArrivalTick);
        }
    }
}