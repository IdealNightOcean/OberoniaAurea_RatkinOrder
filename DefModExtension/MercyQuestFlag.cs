using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestFlag : DefModExtension
{
    public override void ResolveReferences(Def parentDef)
    {
        if (parentDef is QuestScriptDef scriptDef)
        {
            OrderDefDataBase.AddMercyQuests(scriptDef);
        }
    }
}