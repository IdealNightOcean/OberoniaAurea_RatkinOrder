using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class MercyQuestDataBase
{
    private static readonly List<QuestScriptDef> defsList = [];
    public static IReadOnlyList<QuestScriptDef> AllDefsListForReading => defsList;

    private static readonly Dictionary<string, QuestScriptDef> defsByName = [];

    public static void Add(IEnumerable<QuestScriptDef> defs)
    {
        foreach (QuestScriptDef def in defs)
        {
            Add(def);
        }
    }

    public static void Add(QuestScriptDef def)
    {
        if (def is null)
        {
            Log.Error("Tried to add null QuestScriptDef to MercyQuestDataBase.");
            return;
        }
        if (defsByName.ContainsKey(def.defName))
        {
            Log.Error("Adding duplicate MercyQuest name: " + def.defName);
            return;
        }
        defsList.Add(def);
        defsByName.Add(def.defName, def);
        if (defsList.Count > 65535)
        {
            Log.Error("Too many MercyQuest; over " + ushort.MaxValue);
        }
    }
    public static void Clear()
    {
        defsList.Clear();
        defsByName.Clear();
    }

    public static void ReaddAllMercyQuest()
    {
        Clear();
        List<QuestScriptDef> allQuestDefs = DefDatabase<QuestScriptDef>.AllDefsListForReading;
        for (int i = 0; i < allQuestDefs.Count; i++)
        {
            if (allQuestDefs[i].GetModExtension<MercyQuestFlag>() is not null)
            {
                Add(allQuestDefs[i]);
            }
        }
    }

    private static void Remove(QuestScriptDef def)
    {
        defsByName.Remove(def.defName);
        defsList.Remove(def);
    }
}