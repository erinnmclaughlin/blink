using MediatR;

namespace Blink.Web.Components.Pages.Videos.Home.RecentUploads;

public sealed record GetRecentUploadsQuery : IRequest<List<RecentlyUploadedVideoVm>>;
