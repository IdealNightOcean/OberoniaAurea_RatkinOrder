using Verse;

namespace OberoniaAurea.RatkinOrder;

public class TagStrToFloat : TagStrToValue<float>
{
    public TagStrToFloat() : base() { }
    public TagStrToFloat(float defaultValue, bool removeWhenDefault) : base(defaultValue, removeWhenDefault, LookMode.Value) { }

    public override void OffsetTagValueBy(string tag, float offset, bool addIfMiss)
    {
        if (tagStrToValue.TryGetValue(tag, out float newValue))
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

    public override void ScaleTagValueBy(string tag, float scale, bool addIfMiss)
    {
        if (tagStrToValue.TryGetValue(tag, out float newValue))
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