using Microsoft.AspNetCore.Routing;

namespace TaskManagement.Api.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
