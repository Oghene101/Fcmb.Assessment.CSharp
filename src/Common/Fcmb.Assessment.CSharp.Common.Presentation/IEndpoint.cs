using Microsoft.AspNetCore.Routing;

namespace Fcmb.Assessment.CSharp.Common.Presentation;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
