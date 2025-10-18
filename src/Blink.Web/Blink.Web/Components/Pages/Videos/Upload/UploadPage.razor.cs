using System.ComponentModel.DataAnnotations;
using Blink.Storage;
using Blink.Web.Client;
using Blink.Web.Components.Shared;
using Blink.Web.Features.People;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Blink.Web.Components.Pages.Videos.Upload;

public sealed partial class UploadPage
{
    private UploadVideoModel Model { get; set; } = new();
    private IBrowserFile? SelectedFile { get; set; }
    private bool IsUploading { get; set; }
    private bool UploadSuccess { get; set; }
    private int UploadProgress { get; set; }
    private string? ErrorMessage { get; set; }
    private const string DescriptionPlaceholder = "Add a description... (Type @ to mention people)";
    private List<MentionTextarea.MentionData> descriptionMentions = new();
    private List<MentionTextarea.MentionItem> mentionablePeople = new();

    [Inject]
    private IFeatureManager FeatureManager { get; set; } = default!;

    [Inject]
    private IVideoStorageClient VideoStorageClient { get; set; } = default!;

    [Inject]
    private ISender Sender { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<UploadPage> Logger { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (!await FeatureManager.IsEnabledAsync(FeatureFlags.VideoUploads))
        {
            NavigationManager.NavigateTo("/");
        }

        // Load mentionable people from database
        var people = await Sender.Send(new GetPeopleQuery());
        mentionablePeople = people.Select(p => new MentionTextarea.MentionItem
        {
            Id = p.Id,
            Name = p.Name,
            Subtitle = p.Subtitle
        }).ToList();
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

    private void OnDescriptionChanged(string newValue)
    {
        Model.Description = newValue;
    }

    private void OnDescriptionMentionsChanged(List<MentionTextarea.MentionData> mentions)
    {
        descriptionMentions = mentions;
    }

    private async Task CreateNewPeopleAndUpdateMentions()
    {
        // Find all mentions that need new people created
        var newPeople = descriptionMentions
            .Where(m => m.IsNewPerson)
            .ToList();

        if (newPeople.Count == 0)
        {
            Logger.LogInformation("No new people to create");
            return;
        }

        Logger.LogInformation("Creating {Count} new people before saving video", newPeople.Count);

        // Create all new people in the database
        foreach (var mention in newPeople)
        {
            Logger.LogInformation("Creating person: {Name}", mention.Name);
            
            var command = new CreatePersonCommand
            {
                Name = mention.Name
            };

            var personId = await Sender.Send(command);
            
            Logger.LogInformation("Person created with ID: {PersonId}", personId);
            
            // Update the mention ID from temporary to real
            mention.Id = personId.ToString();
            mention.IsNewPerson = false;
        }
        
        Logger.LogInformation("All new people created successfully");
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

            // Create any new people mentioned before saving the video
            await CreateNewPeopleAndUpdateMentions();

            // Register the video in the database via the Web app's handler
            var command = new RegisterUploadedVideoCommand
            {
                BlobName = blobName,
                FileName = SelectedFile.Name,
                SizeInBytes = fileSize,
                Title = Model.Title,
                Description = Model.Description,
                VideoDate = Model.VideoDate,
                DescriptionMentions = descriptionMentions
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

