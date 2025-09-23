using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_EffectTags : QuestPart
{
    private HashSet<string> tags;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref tags, "tags", LookMode.Value);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        tags = null;
    }

    public bool HasTag(string tag) => tags?.Contains(tag) ?? false;

    public void AddTag(string tag)
    {
        if (tags is null)
        {
            tags = [tag];
        }
        else
        {
            tags.Add(tag);
        }
    }

    public void AddTags(IEnumerable<string> tagsToAdd)
    {
        if (tagsToAdd is null)
        {
            return;
        }

        tags ??= [];
        foreach (string t in tagsToAdd)
        {
            tags.Add(t);
        }
    }

    public void RemoveTag(string tag) => tags?.Remove(tag);

    public static bool TryGetEffectTags(Quest quest, bool addPartIfMiss, out QuestPart_EffectTags questPart_EffectTags)
    {
        questPart_EffectTags = quest.PartsListForReading.OfType<QuestPart_EffectTags>()?.FirstOrFallback(null);
        if (addPartIfMiss && questPart_EffectTags is null)
        {
            questPart_EffectTags = new QuestPart_EffectTags();
            quest.AddPart(questPart_EffectTags);
        }
        return questPart_EffectTags is not null;
    }

    public override void DoDebugWindowContents(Rect innerRect, ref float curY)
    {
        Rect rect = new(innerRect.x, curY, 500f, 25f);
        if (Widgets.ButtonText(rect, "Show All Effect Tags"))
        {
            ShowAllQuestEffectTags();
        }

        curY += rect.height + 4f;
    }

    private void ShowAllQuestEffectTags()
    {
        if (tags.NullOrEmpty())
        {
            Messages.Message("No effect tag in this quest.", MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        StringBuilder sb = new();
        foreach (string tag in tags)
        {
            sb.AppendInNewLine(tag);
        }
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(sb.ToTaggedString()));
    }
}