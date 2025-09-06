using System.Xml;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatModifier
{
    public BranchStatDef statDef;
    public BranchStatTransformer Transformer;
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "statDef", xmlRoot);
        Transformer = DirectXmlToObject.ObjectFromXml<BranchStatTransformer>(xmlRoot, doPostLoad: false);
    }

    public void PostLoad()
    {
        Transformer.EnsureFactorMinMagnitude();
    }
}