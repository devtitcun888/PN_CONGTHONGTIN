using Microsoft.AspNetCore.Components.Authorization;
using PN_HDSWeb_Library;

namespace PN_HDSWeb_Admin.Authentication;

public interface IAdminAuthorizationService
{
    Task<bool> IsAdministratorAsync();
    Task<bool> EnsureAdministratorAsync();
}

public class AdminAuthorizationService : IAdminAuthorizationService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AdminAuthorizationService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<bool> IsAdministratorAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = state.User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole("Administrator");
    }

    public async Task<bool> EnsureAdministratorAsync() => await IsAdministratorAsync();
}
