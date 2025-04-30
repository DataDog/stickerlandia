using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.GetUserDetails;
using Stickerlandia.UserManagement.Core.Login;

namespace Stickerlandia.UserManagement.AspNet;

public class GetUserDetailsRequest
{
    [FromHeader] public string Authorization { get; set; } = "";
}

[HttpGet("/details")]
[Authorize]
public class GetUserDetails(GetUserDetailsQueryHandler handler)
    : Endpoint<GetUserDetailsRequest, ApiResponse<UserAccountDTO>?>
{
    public override async Task<UserAccountDTO?> HandleAsync(
        GetUserDetailsRequest req,
        CancellationToken ct)
    {
        var result = await handler.Handle(new GetUserDetailsQuery(req.Authorization));
        
        Response = new ApiResponse<UserAccountDTO>(result);
        return result;
    }
}