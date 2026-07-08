namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatRequestData : StatRequestData<ResidentKnightStatDef, ResidentKnight>
{
    public KnightChivalryDef OtherChivalry { get; set; }


    public ResidentKnightStatRequestData() { }
    public ResidentKnightStatRequestData(ResidentKnight knight, ResidentKnightStatDef statDef) : base(knight, statDef) { }

    public ResidentKnightStatRequestData(ResidentKnight knight, ResidentKnightStatDef statDef, float baseValue) : this(knight, statDef)
    {
        BaseValue = baseValue;
    }

}