using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IThingRequester : ILoadReferenceable
{
    bool IsRequestActive { get; }
    void InitThingRequest(ThingDef requestDef, int requestCount);
    void FulfillRequest(Caravan caravan);
    void DisableRequest();
}