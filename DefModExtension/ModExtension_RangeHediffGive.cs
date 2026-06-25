using OberoniaAurea_Frame;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ModExtension_RangeHediffGive : DefModExtension
{
    public Type giverClass = typeof(RangeHediffGiver);
    public RangeHediffGiveParams giveParams;
}
