namespace Blink.WebApp.Components.Shared.Forms.Button;

public sealed record BlinkButtonTheme
{
    public string Value { get; }

    public static BlinkButtonTheme Default => new("default");
    public static BlinkButtonTheme Primary => new("primary");

    private BlinkButtonTheme(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}
