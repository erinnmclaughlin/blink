using Blink.Storage;
using Blink.Web.Components.Shared;
using Blink.Web.Features.People;
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
    private List<MentionTextarea.MentionItem> mentionablePeople = new();

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

        // Load mentionable people from database
        mentionablePeople = await Sender.Send(new GetPeopleQuery());
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
