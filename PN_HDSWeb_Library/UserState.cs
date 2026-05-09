using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PN_HDSWeb_Library
{
    public class UserState
    {
        public event Action<string> OnNamHocChanged;
        public event Action OnChange;

        private readonly IJSRuntime _jsRuntime;

        public string SessionId { get; private set; }
        public string UserName { get; private set; }
        public string MaTruongBo { get; private set; }
        public string MaUser { get; private set; }
        public string Role { get; private set; }
        public string NamHoc
        {
            get => _namHoc;
            set
            {
                if (_namHoc != value)
                {
                    _namHoc = value;
                    OnNamHocChanged?.Invoke(_namHoc);
                }
            }
        }

        private string _namHoc;
        public bool IsLoading { get; private set; } = true;
        public UserSession CurrentUser { get; private set; }

        public UserState(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync(UserSession userSession)
        {
            IsLoading = true;
            NotifyStateChanged();

            CurrentUser = userSession;
            UpdateSessionInfo(userSession);
            await LoadEssentialData();

            IsLoading = false;
            NotifyStateChanged();
        }

        private void UpdateSessionInfo(UserSession userSession)
        {
            SessionId = userSession.SessionId;
            UserName = userSession.UserName;
            MaTruongBo = userSession.MaTruongBo;
            MaUser = userSession.MaUser;
            Role = userSession.Role;
            NotifyStateChanged();
        }

        public void ClearSession()
        {
            CurrentUser = null;
            UpdateSessionInfo(new UserSession()); // Reset to default values
        }

        private async Task LoadEssentialData()
        {
            // Simulate loading essential data
            await Task.Delay(1000); // Replace with actual data loading
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        // Add methods to check user roles, permissions, etc.
        public bool IsInRole(string role) => Role?.Contains(role, StringComparison.OrdinalIgnoreCase) ?? false;

        public bool IsAuthenticated => CurrentUser != null;
    }
}
