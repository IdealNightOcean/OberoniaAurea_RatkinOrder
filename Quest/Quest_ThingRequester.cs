using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_ThingRequester : QuestNode
{
    public SlateRef<WorldObject> worldObject;
    public SlateRef<ThingDef> requestDef;
    public SlateRef<int> requestCount;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (worldObject.GetValue(slate) is not IThingRequester thingRequester)
        {
            return;
        }
        ThingDef requestDef = this.requestDef.GetValue(slate);
        if (requestDef is null)
        {
            return;
        }

        int requestCount = Mathf.Max(1, this.requestCount.GetValue(slate));
        thingRequester.InitThingRequest(requestDef, requestCount);

        QuestPart_ThingRequester questPart_ThingRequester = new()
        {
            ThingRequester = thingRequester
        };
        QuestGen.quest.AddPart(questPart_ThingRequester);
    }
}

public class QuestPart_ThingRequester : QuestPart
{
    public IThingRequester ThingRequester;
    public override void Cleanup()
    {
        base.Cleanup();
        ThingRequester?.DisableRequest();
        ThingRequester = null;
    }
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ThingRequester, "ThingRequester");
    }
}
