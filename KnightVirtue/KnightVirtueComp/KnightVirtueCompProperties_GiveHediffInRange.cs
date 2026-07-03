using OberoniaAurea_Frame;
using System;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueCompProperties_GiveHediffInRange : KnightVirtueCompProperties
{
    public Type giverClass = typeof(RangeHediffGiver);
    public RangeHediffGiveParams giveParams;
    public int checkInterval = 60;

    public string giverUniqueTag = string.Empty;
    public int giverExcludeInterval;
}
