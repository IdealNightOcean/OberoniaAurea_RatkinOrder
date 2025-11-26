using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderLetterDef : Def
{
    private static readonly Type defaultLetterClass = typeof(OrderLetter);

    public Type letterClass = defaultLetterClass;

    public OrderLetterType letterType;
    public LetterDef relatedLetterDef;
    public bool canShowAsRimLetter = true;
    public bool forceShowAsRimLetter;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (letterClass is null)
        {
            letterClass = defaultLetterClass;
            yield return "has a null 'letterClass'. Set to Default.";
        }
        if (!canShowAsRimLetter && forceShowAsRimLetter)
        {
            forceShowAsRimLetter = false;
            yield return "Cannot 'forceShowAsRimLetter' because 'canShowAsRimLetter' is false";
        }
    }
}
