using NightOcean.Collection;
using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderLetterBox : IExposable
{
    public static OrderLetterBox Instance { get; private set; }

    private readonly int tickHashOffset;

    public bool autoTransNormal;
    public bool autoTransUrgent = true;
    public bool autoTransOfficial = true;

    [Unsaved] private float nextCanClickTime = -1;

    protected List<OrderLetter> delayLetters = []; //待发邮件
    protected List<OrderLetter> unreadLetters = []; // 未读邮件
    protected List<OrderLetter> archivedLetters = []; // 已读邮件

    public List<OrderLetter> ArchivedLetters => archivedLetters;

    public bool HasUnreadLetters => unreadLetters.Count > 0;
    public int ArchivedLettersCount => archivedLetters.Count;

    public OrderLetterBox()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }

    public static void ClearStaticCache() => Instance = null;

    public void Tick()
    {
        if (TickUtility.IsHashIntervalTick(tickHashOffset, 1000))
        {
            TickLong();

            if (TickUtility.IsHashIntervalTick(tickHashOffset, 60000))
            {
                TickDay();
            }
        }
    }

    private void TickLong()
    {
        if (delayLetters.Count > 0)
        {
            int ticksGame = Find.TickManager.TicksGame;
            foreach (OrderLetter letter in delayLetters.ExtractMatching(d => d.ArrivalTick <= ticksGame))
            {
                ReceiveLetter(letter);
            }
        }
    }

    private void TickDay()
    {
        // 优化：避免创建不必要的临时列表，直接在原列表上操作
        int validLetterCount = 0;

        // 第一遍：统计有效信件数量并移动有效信件到列表前部
        for (int i = 0; i < unreadLetters.Count; i++)
        {
            if (!unreadLetters[i].Expired)
            {
                unreadLetters[validLetterCount] = unreadLetters[i];
                validLetterCount++;
            }
        }

        // 如果有过期信件，处理它们
        if (validLetterCount < unreadLetters.Count)
        {
            // 收集过期信件
            List<OrderLetter> expiredLetters = new(unreadLetters.Count - validLetterCount);
            for (int i = validLetterCount; i < unreadLetters.Count; i++)
            {
                expiredLetters.Add(unreadLetters[i]);
            }

            unreadLetters.RemoveRange(validLetterCount, unreadLetters.Count - validLetterCount);
            ListUtils.MergeSortedListsInplace(archivedLetters, expiredLetters, compareFunc: OrderLetterComparerFunc);
            if (RatkinOrderSettings.HasMaxLetterLimit)
            {
                RemoveOvercapArchivedLetters();
            }
        }

        if (RatkinOrderSettings.HasLetterRetentionLimit)
        {
            int maxLetterRetentionDays = RatkinOrderSettings.MaxLetterRetentionDays;
            int retentionTicks = maxLetterRetentionDays * 60000;

            int ticksGame = Find.TickManager.TicksGame;
            archivedLetters.RemoveAll(r => (ticksGame - r.ArrivalTick) >= retentionTicks);
        }
    }

    public void ReceiveLetter(OrderLetter letter, int delayDays = -1)
    {
        if (letter is null)
        {
            return;
        }

        if (delayDays > 0)
        {
            letter.ArrivalTick = Find.TickManager.TicksGame + delayDays * 60000;
            delayLetters.BinaryInsert(letter, compareFunc: OrderLetterComparerFunc);
        }
        else
        {
            letter.ArrivalTick = Find.TickManager.TicksGame;
            unreadLetters.BinaryInsert(letter, compareFunc: OrderLetterComparerFunc);
        }
    }

    public void ReadSingleLetter(OrderLetter letter, Building_OrderLetterBox letterBox, bool forceSlience = false)
    {
        try
        {
            OrderLetterUtility.ReadLetter(letter, letterBox, forceSlience);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex, errorDesc: "读取信件", typeName: nameof(OrderLetterBox), methodName: nameof(ReadSingleLetter));
        }
        finally
        {
            ArchiveLetter(letter);
        }
    }

    public void ReadAllUnreadLetters(Building_OrderLetterBox letterBox, bool forceSlience = false)
    {

        for (int i = 0; i < unreadLetters.Count; i++)
        {
            try
            {
                OrderLetterUtility.ReadLetter(unreadLetters[i], letterBox, forceSlience);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex, errorDesc: "读取信件", typeName: nameof(OrderLetterBox), methodName: nameof(ReadAllUnreadLetters));
            }
        }

        ClearAllUnreadLetters();
    }

    public void ClearAllUnreadLetters()
    {
        ListUtils.MergeSortedListsInplace(archivedLetters, unreadLetters, compareFunc: OrderLetterComparerFunc);
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
        archivedLetters.BinaryInsert(letter, compareFunc: OrderLetterComparerFunc);
    }

    /// <summary>
    /// 降序比较方法
    /// </summary>
    private static int OrderLetterComparerFunc(OrderLetter a, OrderLetter b) => b.ArrivalTick.CompareTo(a.ArrivalTick);

    public void ExposeData()
    {
        Scribe_Values.Look(ref autoTransNormal, nameof(autoTransNormal), defaultValue: false);
        Scribe_Values.Look(ref autoTransOfficial, nameof(autoTransOfficial), defaultValue: true);
        Scribe_Values.Look(ref autoTransUrgent, nameof(autoTransUrgent), defaultValue: true);

        Scribe_Collections.Look(ref delayLetters, nameof(delayLetters), LookMode.Deep);
        Scribe_Collections.Look(ref unreadLetters, nameof(unreadLetters), LookMode.Deep);
        Scribe_Collections.Look(ref archivedLetters, nameof(archivedLetters), LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            delayLetters.RemoveAll(l => l is null);
            unreadLetters.RemoveAll(l => l is null);
            archivedLetters.RemoveAll(l => l is null);

            delayLetters.OrderBy(r => -r.ArrivalTick);
            unreadLetters.OrderBy(r => -r.ArrivalTick);
            archivedLetters.OrderBy(r => -r.ArrivalTick);
        }
    }
}