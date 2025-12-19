using System.ComponentModel.DataAnnotations;
using Blink.Web.FeatureManagement;
using Blink.Web.Videos.Requests;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FeatureManagement;

namespace Blink.Web.Videos.Components.Pages.Upload;

public sealed partial class UploadPage
{
    private readonly IFeatureManager _featureManager;
    private readonly NavigationManager _navigationManager;
    private readonly ISender _sender;
    
    private UploadVideoModel Model { get; set; } = new();
    private bool IsUploading { get; set; }
    private int UploadProgress { get; set; }
    private string? ErrorMessage { get; set; }

    public UploadPage(IFeatureManager featureManager, NavigationManager navigationManager, ISender sender)
    {
        _featureManager = featureManager;
        _navigationManager = navigationManager;
        _sender = sender;
    }
    
    protected override async Task OnInitializedAsync()
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.VideoUploads))
        {
            _navigationManager.NavigateTo("/");
        }
    }

    private void HandleFileSelected(InputFileChangeEventArgs e)
    {
        Model.File = e.File;
        ErrorMessage = null;

        // Populate title from filename if not already set
        if (string.IsNullOrWhiteSpace(Model.Title) && Model.File != null)
        {
            Model.Title = Path.GetFileNameWithoutExtension(Model.File.Name);
        }
    }

    private void ClearFile()
    {
        Model.File = null;
    }

    private async Task HandleSubmit()
    {
        if (IsUploading)
        {
            return;
        }
        
        if (Model.File == null)
        {
            ErrorMessage = "Please select a video file to upload.";
            return;
        }

        IsUploading = true;
        UploadProgress = 0;
        ErrorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            // Start progress simulation
            var progressCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                for (var i = 0; i <= 90; i += 5)
                {
                    if (progressCts.Token.IsCancellationRequested) break;
                    UploadProgress = i;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(300, progressCts.Token);
                }
            }, progressCts.Token);

            // Upload
            var videoId = await _sender.Send(new UploadVideo.Command
            {
                VideoFile = Model.File,
                Title = Model.Title ?? Model.File.Name,
                Description = Model.Description,
                VideoDate = Model.VideoDate
            }, CancellationToken.None);

            // Stop progress simulation
            await progressCts.CancelAsync();
            UploadProgress = 95;
            await InvokeAsync(StateHasChanged);

            // Complete
            UploadProgress = 100;
            await InvokeAsync(StateHasChanged);
            
            IsUploading = false;
            
            _navigationManager.NavigateTo($"videos/{videoId}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            IsUploading = false;
        }
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

