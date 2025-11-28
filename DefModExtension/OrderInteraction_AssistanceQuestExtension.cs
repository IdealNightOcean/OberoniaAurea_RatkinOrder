using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteraction_AssistanceQuestExtension : DefModExtension
{
    public QuestScriptDef assistanceQuest;
    public PawnKindDef assistantPawnkind;
    public int assistantCount = 1;
    public ThoughtDef thoughtToAdd;

    public void SetSlateValue(Slate slate)
    {
        slate.Set("assistantPawnkind", assistantPawnkind);
        slate.Set("assistantCount", assistantCount);
        if (thoughtToAdd is not null)
        {
            slate.Set("thoughtToAdd", thoughtToAdd);
        }
    }
}