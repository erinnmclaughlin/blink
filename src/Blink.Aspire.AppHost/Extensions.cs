namespace Blink.AppHost;

public static class Extensions
{
    public static IResourceBuilder<ProjectResource> WithAwaitedReference(this IResourceBuilder<ProjectResource> builder, IResourceBuilder<IResourceWithConnectionString> resource)
    {
        return builder.WithReference(resource).WaitFor(resource);
    }

    public static IResourceBuilder<ProjectResource> WithAwaitedReference(this IResourceBuilder<ProjectResource> builder, IResourceBuilder<IResourceWithServiceDiscovery> resource)
    {
        return builder.WithReference(resource).WaitFor(resource);
    }
}