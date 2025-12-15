using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_EffectTags : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        QuestGen.quest.AddPart(new QuestPart_EffectTags());
    }
}

public class QuestPart_EffectTags : QuestPart
{
    private Dictionary<string, QuestEffectTag> tags;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref tags, "tags", LookMode.Value, LookMode.Deep);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        tags = null;
    }

    public bool HasTag(string tagKey) => tags?.ContainsKey(tagKey) ?? false;

    public void AddTag(QuestEffectTag tag)
    {
        if (tags is null)
        {
            tags = new Dictionary<string, QuestEffectTag> { { tag.Key, tag } };

        }
        else
        {
            tags.Add(tag.Key, tag);
        }
    }

    public void AddTags(IEnumerable<QuestEffectTag> tagsToAdd)
    {
        if (tagsToAdd is null)
        {
            return;
        }

        tags ??= [];
        foreach (QuestEffectTag t in tagsToAdd)
        {
            tags.Add(t.Key, t);
        }
    }

    public void RemoveTag(string tagKey)
    {
        tags?.Remove(tagKey);
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
        foreach (QuestEffectTag tag in tags.Values)
        {
            sb.AppendInNewLine(tag.ToString());
        }
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(sb.ToTaggedString()));
    }
}