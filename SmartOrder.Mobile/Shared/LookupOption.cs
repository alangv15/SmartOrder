namespace SmartOrder.Mobile.Shared;

public sealed class LookupOption
{
    public LookupOption(string label, string code)
    {
        Label = label;
        Code = code;
    }

    public string Label { get; }
    public string Code { get; }
}
