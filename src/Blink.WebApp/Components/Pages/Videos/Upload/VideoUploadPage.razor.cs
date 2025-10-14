using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Blink.WebApp.Components.Pages.Videos.Upload;

public sealed partial class VideoUploadPage : ComponentBase, IAsyncDisposable
{
    [Inject] private BlinkApiClient ApiClient { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private IJSObjectReference? _uploadModule;
    private IJSObjectReference? _uploadManager;
    private DotNetObjectReference<VideoUploadPage>? _dotNetReference;

    private string? _videoTitle;
    private string? _videoDescription;
    private DateTime? _videoDate;
    private long _selectedFileSize;
    private string? _selectedFileName;

    private bool _isUploading;
    private int _uploadProgress;
    private string? _uploadStatus;
    private string? _errorMessage;
    private string? _uploadSpeed;
    private string? _estimatedTimeRemaining;
    private DateTime? _uploadStartTime;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetReference = DotNetObjectReference.Create(this);
        }

        return Task.CompletedTask;
    }

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        _errorMessage = null;
        _selectedFileName = null;
        _selectedFileSize = 0;

        var file = e.File;
        
        if (file != null)
        {
            _selectedFileName = file.Name;
            _selectedFileSize = file.Size;

            if (_selectedFileSize > 2_000_000_000) // 2GB limit
            {
                _errorMessage = "File size exceeds 2GB limit.";
                _selectedFileName = null;
                _selectedFileSize = 0;
            }
        }
    }

    private async Task UploadVideo()
    {
        if (string.IsNullOrEmpty(_selectedFileName))
        {
            _errorMessage = "Please select a video file.";
            return;
        }

        _isUploading = true;
        _uploadProgress = 0;
        _uploadStatus = "Requesting upload URL...";
        _errorMessage = null;
        _uploadStartTime = DateTime.UtcNow;
        StateHasChanged();

        try
        {
            // Step 1: Request upload URL from server
            var initiateResponse = await ApiClient.InitiateUploadAsync(_selectedFileName);
            
            _uploadStatus = "Uploading to cloud storage...";
            StateHasChanged();

            // Step 2: Upload directly to blob storage using JavaScript
            _uploadModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/directUpload.js");
            _uploadManager = await JSRuntime.InvokeAsync<IJSObjectReference>("eval", "new DirectUploadManager()");

            await JSRuntime.InvokeVoidAsync(
                "uploadFileDirectly",
                "videoFileInput",
                initiateResponse.UploadUrl,
                _dotNetReference,
                "OnUploadProgress"
            );

            _uploadStatus = "Finalizing...";
            _uploadProgress = 100;
            StateHasChanged();

            // Step 3: Notify server that upload is complete
            DateOnly? videoDate = _videoDate.HasValue ? DateOnly.FromDateTime(_videoDate.Value) : null;
            var completeResponse = await ApiClient.CompleteUploadAsync(
                initiateResponse.BlobName,
                _selectedFileName,
                _videoTitle,
                _videoDescription,
                videoDate
            );

            _uploadStatus = "Upload complete!";
            StateHasChanged();

            // Redirect to video detail page
            await Task.Delay(500); // Brief delay to show success message
            Navigation.NavigateTo($"/videos/watch/{initiateResponse.BlobName}");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Upload failed: {ex.Message}";
            _uploadStatus = null;
            _isUploading = false;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnUploadProgress(int percentage, long bytesUploaded, long totalBytes)
    {
        _uploadProgress = percentage;

        // Calculate upload speed and ETA
        if (_uploadStartTime.HasValue)
        {
            var elapsed = (DateTime.UtcNow - _uploadStartTime.Value).TotalSeconds;
            if (elapsed > 0)
            {
                var bytesPerSecond = bytesUploaded / elapsed;
                _uploadSpeed = FormatSpeed(bytesPerSecond);

                var bytesRemaining = totalBytes - bytesUploaded;
                var secondsRemaining = bytesRemaining / bytesPerSecond;
                _estimatedTimeRemaining = FormatDuration(secondsRemaining);
            }
        }

        InvokeAsync(StateHasChanged);
    }

    private void CancelUpload()
    {
        _isUploading = false;
        _uploadProgress = 0;
        _uploadStatus = null;
        _uploadSpeed = null;
        _estimatedTimeRemaining = null;
        StateHasChanged();
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:F0} B/s";
        if (bytesPerSecond < 1024 * 1024)
            return $"{bytesPerSecond / 1024:F1} KB/s";
        if (bytesPerSecond < 1024 * 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        return $"{bytesPerSecond / (1024 * 1024 * 1024):F1} GB/s";
    }

    private static string FormatDuration(double seconds)
    {
        if (seconds < 60)
            return $"{seconds:F0}s";
        if (seconds < 3600)
            return $"{seconds / 60:F0}m {seconds % 60:F0}s";
        return $"{seconds / 3600:F0}h {(seconds % 3600) / 60:F0}m";
    }

    public async ValueTask DisposeAsync()
    {
        if (_uploadModule != null)
            await _uploadModule.DisposeAsync();
        
        if (_uploadManager != null)
            await _uploadManager.DisposeAsync();

        _dotNetReference?.Dispose();
    }
}

