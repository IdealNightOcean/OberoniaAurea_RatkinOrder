using System.Xml;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatModifier
{
    public BranchStatDef statDef;
    public BranchStatTransformer Transformer;

    public string ModifySummary() => Transformer.TransSummary(statDef);

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "statDef", xmlRoot);
        Transformer = DirectXmlToObject.ObjectFromXml<BranchStatTransformer>(xmlRoot, doPostLoad: false);
    }
}