using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blink.Web.Components.Shared;

public sealed partial class MentionTextarea : IAsyncDisposable
{
    private readonly DotNetObjectReference<MentionTextarea> _dotNetRef;
    private readonly string _elementId = $"mention-textarea-{Guid.NewGuid():N}";
    private readonly IJSRuntime _js;
    
    private string _currentValue = string.Empty;
    private IJSObjectReference? _tributeModule;

    [Parameter] public string AriaLabel { get; set; } = "Text input";

    [Parameter] public string Placeholder { get; set; } = "Type @ to mention someone...";

    [Parameter] public List<MentionItem> MentionItems { get; set; } = [];

    [Parameter] public EventCallback<List<MentionMetadata>> MentionsChanged { get; set; }

    [Parameter] public int Rows { get; set; } = 3;

    [Parameter] public string Value { get; set; } = "";

    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    public MentionTextarea(IJSRuntime js)
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        _js = js;
    }
    
    protected override void OnParametersSet()
    {
        _currentValue = Value;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _tributeModule = await _js.InvokeAsync<IJSObjectReference>("import", "./Components/Shared/MentionTextarea.razor.js");
            await _tributeModule.InvokeVoidAsync("initializeTribute", _elementId, _dotNetRef, MentionItems);
        }
    }

    [JSInvokable]
    public async Task OnInputChanged(string newValue, List<MentionMetadata>? mentions)
    {
        _currentValue = newValue;
        await ValueChanged.InvokeAsync(newValue);

        if (mentions != null)
        {
            await MentionsChanged.InvokeAsync(mentions);
        }
    }

    private async Task OnBlur()
    {
        if (_tributeModule != null)
        {
            var value = await _tributeModule.InvokeAsync<string>("getContentEditableText", _elementId);
            _currentValue = value;
            await ValueChanged.InvokeAsync(value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_tributeModule != null)
        {
            await _tributeModule.InvokeVoidAsync("disposeTribute", _elementId);
            await _tributeModule.DisposeAsync();
        }

        _dotNetRef.Dispose();
    }
    
    public sealed class MentionItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Subtitle { get; set; }
    }
}