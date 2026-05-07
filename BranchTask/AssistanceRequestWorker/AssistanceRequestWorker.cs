using OberoniaAurea_Frame;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public abstract class AssistanceRequestWorker
{
    public abstract void Initialize(AssistanceRequest request, List<KnightAcademicDef> dutyAcademics);
    public abstract float CalculateDailyProgress(FixedCaravan fixedCaravan, AssistanceRequest request);
}
