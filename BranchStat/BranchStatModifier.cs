using System.Xml;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatModifier
{
    public BranchStatDef statDef;
    public BranchStatTransformer Transformer;

    public string ModifierExplanation()
    {
        return statDef.statType switch
        {
            BranchStatDef.StatType.Percent =>
                statDef.label
                + $": {Transformer.offset.ToStringPercentSigned("F2").Colorize((Transformer.offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}"
                + $" / {Transformer.factor.ToStringPercentSigned("F2").Colorize((Transformer.factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}"
                + $" / {Transformer.fixedOffset.ToStringPercentSigned("F2").Colorize((Transformer.fixedOffset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}",

            _ =>
                statDef.label
                + $": {Transformer.offset.ToStringWithSign("F2").Colorize((Transformer.offset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}"
                + $" / {Transformer.factor.ToStringWithSign("F2").Colorize((Transformer.factor < 1f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}"
                + $" / {Transformer.fixedOffset.ToStringWithSign("F2").Colorize((Transformer.fixedOffset < 0f ^ statDef.reverse) ? ColorLibrary.RedReadable : Color.green)}",
        };
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "statDef", xmlRoot);
        Transformer = DirectXmlToObject.ObjectFromXml<BranchStatTransformer>(xmlRoot, doPostLoad: false);
    }
}