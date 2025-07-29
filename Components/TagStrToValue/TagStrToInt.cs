using Verse;

namespace OberoniaAurea.RatkinOrder;

public class TagStrToInt : TagStrToValue<int>
{
    public TagStrToInt() : base() { }
    public TagStrToInt(int defaultValue, bool removeWhenDefault) : base(defaultValue, removeWhenDefault, LookMode.Value) { }

    public override void OffsetTagValueBy(string tag, int offset, bool addIfMiss)
    {
        if (tagStrToValue.TryGetValue(tag, out int newValue))
        {
            newValue += offset;
            if (newValue == defaultValue && removeWhenDefault)
            {
                tagStrToValue.Remove(tag);
            }
            else
            {
                tagStrToValue[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = defaultValue + offset;
            if (newValue == defaultValue && removeWhenDefault)
            {
                return;
            }
            tagStrToValue[tag] = newValue;
        }
    }

    public override void ScaleTagValueBy(string tag, int scale, bool addIfMiss)
    {
        if (tagStrToValue.TryGetValue(tag, out int newValue))
        {
            newValue *= scale;
            if (newValue == defaultValue && removeWhenDefault)
            {
                tagStrToValue.Remove(tag);
            }
            else
            {
                tagStrToValue[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = defaultValue * scale;
            if (newValue == defaultValue && removeWhenDefault)
            {
                return;
            }
            tagStrToValue[tag] = newValue;
        }
    }
}
