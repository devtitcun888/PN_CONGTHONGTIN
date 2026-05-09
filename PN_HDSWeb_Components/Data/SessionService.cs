using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using PN_HDSWeb_Library;

namespace PN_HDSWeb_Components.Data
{
        public class SessionService
        {
            //private readonly ProtectedSessionStorage _sessionStorage;
            //private readonly IJSRuntime _jsRuntime;

            //public SessionService(ProtectedSessionStorage sessionStorage, IJSRuntime jsRuntime)
            //{
            //    _sessionStorage = sessionStorage;
            //    _jsRuntime = jsRuntime;
            //}

            //public event Action OnChange;
            //public bool IsLoading { get; private set; } = true;
            //public UserSession CurrentUser { get; private set; }

            //public async Task InitializeAsync()
            //{
            //    IsLoading = true;
            //    NotifyStateChanged();

            //    var result = await _sessionStorage.GetAsync<UserSession>("UserSession");
            //    if (result.Success)
            //    {
            //        CurrentUser = result.Value;
            //    }

            //    IsLoading = false;
            //    NotifyStateChanged();
            //}

            //public async Task SetUserSessionAsync(UserSession userSession)
            //{
            //    CurrentUser = userSession;
            //    await _sessionStorage.SetAsync("UserSession", userSession);
            //    NotifyStateChanged();
            //}

            //public async Task ClearSessionAsync()
            //{
            //    CurrentUser = null;
            //    await _sessionStorage.DeleteAsync("UserSession");
            //    NotifyStateChanged();
            //}

            //public bool IsAuthenticated => CurrentUser != null;

            //private void NotifyStateChanged() => OnChange?.Invoke();
        }
}
