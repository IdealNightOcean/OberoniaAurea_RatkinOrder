using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueCompProperties
{
    [TranslationHandle]
    public Type compClass;

    public virtual void PostLoad() { }

    public virtual void ResolveReferences(KnightVirtueDef parent) { }
}
