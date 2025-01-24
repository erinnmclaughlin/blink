namespace Blink.WebApp.Components.Shared.Forms.Button;

public sealed record BlinkButtonType
{
    public string Value { get; }

    public static BlinkButtonType Button => new("button");
    public static BlinkButtonType Reset => new("reset");
    public static BlinkButtonType Submit => new("submit");

    private BlinkButtonType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}
