using System.ComponentModel.DataAnnotations;
using Blink.Storage;
using Blink.VideosApi.Contracts.Upload;
using Blink.Web.Client;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Blink.Web.Components.Pages.Upload;

public sealed partial class UploadPage
{
    private UploadVideoModel Model { get; set; } = new();
    private IBrowserFile? SelectedFile { get; set; }
    private bool IsUploading { get; set; }
    private bool UploadSuccess { get; set; }
    private int UploadProgress { get; set; }
    private string? ErrorMessage { get; set; }

    [Inject]
    private IFeatureFlagManager FeatureFlagManager { get; set; } = default!;

    [Inject]
    private IPublishEndpoint PublishEndpoint { get; set; } = default!;

    [Inject]
    private IVideoStorageClient VideoStorageClient { get; set; } = default!;

    [Inject]
    private ISender Sender { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (!await FeatureFlagManager.IsEnabledAsync(FeatureFlags.Uploads))
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private void HandleFileSelected(InputFileChangeEventArgs e)
    {
        SelectedFile = e.File;
        ErrorMessage = null;

        // Auto-populate title from filename if not already set
        if (string.IsNullOrWhiteSpace(Model.Title) && SelectedFile != null)
        {
            Model.Title = Path.GetFileNameWithoutExtension(SelectedFile.Name);
        }
    }

    private void ClearFile()
    {
        SelectedFile = null;
        Model.File = null;
    }

    private async Task HandleSubmit()
    {
        if (SelectedFile == null)
        {
            ErrorMessage = "Please select a video file to upload.";
            return;
        }

        IsUploading = true;
        UploadProgress = 0;
        ErrorMessage = null;

        try
        {
            // Start progress simulation
            var progressCts = new CancellationTokenSource();
            var progressTask = Task.Run(async () =>
            {
                for (int i = 0; i <= 90; i += 5)
                {
                    if (progressCts.Token.IsCancellationRequested) break;
                    UploadProgress = i;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(300, progressCts.Token);
                }
            }, progressCts.Token);

            // Upload directly to Azure Storage
            var maxFileSize = 2000L * 1024 * 1024; // 2000MB
            using var stream = SelectedFile.OpenReadStream(maxFileSize);
            
            var (blobName, fileSize) = await VideoStorageClient.UploadAsync(
                stream, 
                SelectedFile.Name, 
                CancellationToken.None);

            // Stop progress simulation
            progressCts.Cancel();
            UploadProgress = 95;
            await InvokeAsync(StateHasChanged);

            // Register the video in the database via the Web app's handler
            var command = new RegisterUploadedVideoCommand
            {
                BlobName = blobName,
                FileName = SelectedFile.Name,
                SizeInBytes = fileSize,
                Title = Model.Title,
                Description = Model.Description,
                VideoDate = Model.VideoDate
            };

            await Sender.Send(command);

            // Complete
            UploadProgress = 100;
            await InvokeAsync(StateHasChanged);
            
            UploadSuccess = true;
            IsUploading = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            IsUploading = false;
        }
    }

    private void ResetForm()
    {
        Model = new UploadVideoModel();
        SelectedFile = null;
        IsUploading = false;
        UploadSuccess = false;
        UploadProgress = 0;
        ErrorMessage = null;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        else if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        else
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    private sealed class UploadVideoModel
    {
        public IBrowserFile? File { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime? VideoDate { get; set; }
    }
}

