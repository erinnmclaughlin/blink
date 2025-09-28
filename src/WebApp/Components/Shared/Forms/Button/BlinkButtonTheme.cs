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

public sealed record BlinkButtonSize
{
    public string Value { get; }

    public static BlinkButtonSize Default => Medium;

    public static BlinkButtonSize Small => new("sm");
    public static BlinkButtonSize Medium => new("md");
    // TODO: public static BlinkButtonSize Large => new("large");

    private BlinkButtonSize(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}