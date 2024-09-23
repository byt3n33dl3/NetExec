using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SharpC2.API;
using SharpC2.API.Responses;

using TeamServer.Interfaces;

namespace TeamServer.Controllers;

[Authorize]
[ApiController]
[Route(Routes.V1.Profiles)]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profiles;

    public ProfilesController(IProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<C2ProfileResponse>>> GetProfiles()
    {
        var profiles = await _profiles.Get();
        var response = profiles.Select(p => (C2ProfileResponse)p);
        
        return Ok(response);
    }
    
    [HttpGet("{name}")]
    public async Task<ActionResult<C2ProfileResponse>> GetProfile(string name)
    {
        var profile = await _profiles.Get(name);

        if (profile is null)
            return NotFound();
        
        return Ok((C2ProfileResponse)profile);
    }
}