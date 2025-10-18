using Blink.Storage;
using Blink.Web.Components.Shared;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace Blink.Web.Components.Pages.Videos.Watch;

public sealed partial class VideoDetailPage
{
    private VideoDetailVm? Video { get; set; }
    private string? ThumbnailUrl { get; set; }
    private string? WatchUrl { get; set; }
    private string commentText = string.Empty;
    private const string CommentPlaceholder = "Add a comment... (Type @ to mention someone)";
    
    // Sample people for mentions - in a real app, this would come from a user service
    private readonly List<MentionTextarea.MentionItem> mentionablePeople = new()
    {
        new() { Id = "1", Name = "Erin McLaughlin", Subtitle = "Video Owner" },
        new() { Id = "2", Name = "John Doe", Subtitle = "Team Member" },
        new() { Id = "3", Name = "Alex Martinez", Subtitle = "Collaborator" },
        new() { Id = "4", Name = "Sarah Kim", Subtitle = "Team Member" },
        new() { Id = "5", Name = "Lisa Thompson", Subtitle = "Viewer" }
    };

    [Parameter]
    public Guid VideoId { get; set; }

    [Inject]
    private ISender Sender { get; set; } = default!;

    [Inject]
    private IVideoStorageClient VideoStorageClient { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Video = await Sender.Send(new GetVideoDetailQuery(VideoId));

        if (Video is not null)
        {
            WatchUrl = await VideoStorageClient.GetUrlAsync(Video.BlobName);
        }

        if (Video?.ThumbnailBlobName is not null)
        {
            ThumbnailUrl = await VideoStorageClient.GetThumbnailUrlAsync(Video.ThumbnailBlobName);
        }
    }
    
    private void OnCommentTextChanged(string newValue)
    {
        commentText = newValue;
    }
    
    private void PostComment()
    {
        // TODO: Implement comment posting logic
        if (string.IsNullOrWhiteSpace(commentText))
        {
            return;
        }
        
        // Extract mentions from the comment text (simple regex)
        // In production, you'd want to parse and store these properly
        Console.WriteLine($"Posting comment: {commentText}");
        
        // Reset the comment text
        commentText = string.Empty;
        StateHasChanged();
    }
}
