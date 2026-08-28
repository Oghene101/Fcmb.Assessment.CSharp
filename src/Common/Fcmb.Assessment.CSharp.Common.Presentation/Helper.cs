using Asp.Versioning;
using Microsoft.AspNetCore.Http;

namespace Fcmb.Assessment.CSharp.Common.Presentation;

public static class Helper
{
    public static Uri BuildUri(HttpContext context, string endpoint)
    {
        IApiVersioningFeature? feature = context.Features.Get<IApiVersioningFeature>();
        int apiVersion = feature?.RequestedApiVersion?.MajorVersion ?? 1;

        return new UriBuilder
        {
            Scheme = context.Request.Scheme,
            Host = context.Request.Host.Host,
            Port = context.Request.Host.Port ?? -1, // -1 omits the port if not present
            Path = $"/api/v{apiVersion}/{endpoint}"
        }.Uri;
    }
}
