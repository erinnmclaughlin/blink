using System.ComponentModel.DataAnnotations;
using Blink.Storage;
using Blink.Web.FeatureManagement;
using Blink.Web.Mentions;
using Blink.Web.Mentions.Components;
using Blink.Web.Mentions.Features;
using Blink.Web.Videos.Features;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FeatureManagement;

namespace Blink.Web.Videos.Components.Pages.Upload;

public sealed partial class UploadPage
{
    private UploadVideoModel Model { get; set; } = new();
    private IBrowserFile? SelectedFile { get; set; }
    private bool IsUploading { get; set; }
    private int UploadProgress { get; set; }
    private string? ErrorMessage { get; set; }
    private const string DescriptionPlaceholder = "Add a description... (Type @ to mention people)";
    private List<MentionMetadata> descriptionMentions = new();
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

    private void OnDescriptionChanged(string? newValue)
    {
        Model.Description = newValue;
    }

    private void OnDescriptionMentionsChanged(List<MentionMetadata> mentions)
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
            return;
        }

        // Create all new people in the database
        foreach (var mention in newPeople)
        {
            var command = new CreatePersonCommand
            {
                Name = mention.Name
            };

            var personId = await Sender.Send(command);
            
            // Update the mention ID from temporary to real
            mention.Id = personId.ToString();
            mention.IsNewPerson = false;
        }
        
        Logger.LogInformation("All new people created successfully");
    }

    private async Task HandleSubmit()
    {
        if (IsUploading)
        {
            return;
        }
        
        if (SelectedFile == null)
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
            var videoId = await Sender.Send(new UploadVideo.Command
            {
                VideoFile = SelectedFile,
                Title = Model.Title ?? SelectedFile.Name,
                Description = Model.Description,
                //DescriptionMentions = descriptionMentions,
                VideoDate = Model.VideoDate.HasValue ? DateOnly.FromDateTime(Model.VideoDate.Value) : null
            }, CancellationToken.None);

            // Stop progress simulation
            await progressCts.CancelAsync();
            UploadProgress = 95;
            await InvokeAsync(StateHasChanged);

            // Create any new people mentioned
            await CreateNewPeopleAndUpdateMentions();

            // Complete
            UploadProgress = 100;
            await InvokeAsync(StateHasChanged);
            
            IsUploading = false;
            
            NavigationManager.NavigateTo($"videos/{videoId}");
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

